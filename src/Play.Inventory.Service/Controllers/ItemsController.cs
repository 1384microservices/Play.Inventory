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
    private readonly IRepository<InventoryItem> _repository;
    private readonly CatalogClient _catalogClient;

    public ItemsController(IRepository<InventoryItem> repository, CatalogClient catalogClient)
    {
        _repository = repository;
        _catalogClient = catalogClient;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<InventoryItemDto>>> GetAsync(Guid userId)
    {
        if (userId == Guid.Empty)
            return BadRequest();

        var catalogItems = await _catalogClient.GetCatalogItemsAsync();
        var inventoryItems = await _repository.GetAllAsync(item => item.UserId == userId);
        var dtos = inventoryItems.Select(item =>
        {
            var catalogItem = catalogItems.Single(x => x.Id == item.CatalogItemId);
            return item.AsDto(catalogItem.Name, catalogItem.Description);
        });

        return Ok(dtos);
    }

    [HttpPost]
    public async Task<ActionResult> PostAsync(GrantItemDto item)
    {
        var inventoryItem = await _repository.GetOneAsync(
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

            await _repository.CreateAsync(inventoryItem);
        }
        else
        {
            inventoryItem.Quantity += item.Quantity;
            await _repository.UpdateAsync(inventoryItem);
        }
        return Ok();
    }
}