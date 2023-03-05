using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Play.Common;
using Play.Inventory.Service.Clients;
using Play.Inventory.Service.Dtos;
using Play.Inventory.Service.Entities;

namespace Play.Inventory.Service.Controllers;


[ApiController]
[Route("items")]
public class ItemsController : ControllerBase
{
    private readonly IRepository<InventoryItem> _inventoryItemRepository;
    private readonly IRepository<CatalogItem> _catalogItemRepository;

    public ItemsController(IRepository<InventoryItem> repository, IRepository<CatalogItem> catalogItemRepository)
    {
        _inventoryItemRepository = repository;
        _catalogItemRepository = catalogItemRepository;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<InventoryItemDto>>> GetAsync(Guid userId)
    {
        if (userId == Guid.Empty)
        {
            return BadRequest();
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
        return Ok();
    }
}