using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using MassTransit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Play.Common;
using Play.Inventory.Contracts;
using Play.Inventory.Service.Dtos;
using Play.Inventory.Service.Entities;

namespace Play.Inventory.Service.Controllers;


[ApiController]
[Route("items")]
public class ItemsController : ControllerBase
{
    private const string AdminRole = "Admin";
    private readonly IRepository<InventoryItem> _inventoryItemRepository;
    private readonly IRepository<CatalogItem> _catalogItemRepository;
    private readonly IPublishEndpoint _publishEndpoint;

    public ItemsController(IRepository<InventoryItem> repository, IRepository<CatalogItem> catalogItemRepository, IPublishEndpoint publishEndpoint)
    {
        _inventoryItemRepository = repository;
        _catalogItemRepository = catalogItemRepository;
        _publishEndpoint = publishEndpoint;
    }

    [HttpGet]
    [Authorize]
    public async Task<ActionResult<IEnumerable<InventoryItemDto>>> GetAsync(Guid userId)
    {
        if (userId == Guid.Empty)
        {
            return BadRequest();
        }

        var currentUserId = User.FindFirstValue(JwtRegisteredClaimNames.Sub);

        if (Guid.Parse(currentUserId) != userId)
        {
            if (!User.IsInRole(AdminRole))
            {
                return Forbid();
            }
        }

        var inventoryItems = await _inventoryItemRepository.GetAllAsync(item => item.UserId == userId);
        var inventoryItemIds = inventoryItems.Select(item => item.CatalogItemId);
        var catalogItems = await _catalogItemRepository.GetAllAsync(x => inventoryItemIds.Contains(x.Id));
        var dtos = inventoryItems.Select(inventoryItem =>
        {
            var catalogItem = catalogItems.Single(x => x.Id == inventoryItem.CatalogItemId);
            return inventoryItem.AsDto(catalogItem.Name, catalogItem.Description);
        });

        return Ok(dtos);
    }

    [HttpPost]
    [Authorize(Roles = AdminRole)]
    public async Task<ActionResult> PostAsync(GrantItemDto item)
    {
        var inventoryItem = await _inventoryItemRepository.GetOneAsync(
            x => x.UserId == item.UserId &&
            x.CatalogItemId == item.CatalogItemId);

        if (inventoryItem is null)
        {
            inventoryItem = new InventoryItem
            {
                CatalogItemId = item.CatalogItemId,
                UserId = item.UserId,
                Quantity = item.Quantity,
                AcquireDate = DateTimeOffset.UtcNow
            };

            await _inventoryItemRepository.CreateAsync(inventoryItem);
        }
        else
        {
            inventoryItem.Quantity += item.Quantity;
            await _inventoryItemRepository.UpdateAsync(inventoryItem);
        }


        await _publishEndpoint.Publish(new InventoryItemUpdated(inventoryItem.UserId, inventoryItem.CatalogItemId, inventoryItem.Quantity));
        return Ok();
    }
}