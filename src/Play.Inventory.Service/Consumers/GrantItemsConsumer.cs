using System;
using System.Threading.Tasks;
using MassTransit;
using Microsoft.Extensions.Logging;
using Play.Common;
using Play.Inventory.Contracts;
using Play.Inventory.Service.Entities;
using Play.Inventory.Service.Exceptions;

namespace Play.Inventory.Service.Consumers;

public class GrantItemsConsumer : IConsumer<GrantItems>
{
    private readonly IRepository<InventoryItem> _inventoryItemRepository;
    private readonly IRepository<CatalogItem> _catalogItemRepository;
    private readonly ILogger<GrantItemsConsumer> _logger;

    public GrantItemsConsumer(
        IRepository<InventoryItem> inventoryItemRepository, 
        IRepository<CatalogItem> catalogItemRepository, 
        ILogger<GrantItemsConsumer> logger)
    {
        _inventoryItemRepository = inventoryItemRepository;
        _catalogItemRepository = catalogItemRepository;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<GrantItems> context)
    {
        
        var message = context.Message;
        _logger.LogInformation(
            "Grant {Quantity} of item {CatalogItemId} for user {UserId} with {CorrelationId}", 
            message.Quantity, 
            message.CatalogItemId, 
            message.UserId, 
            message.CorrelationId);

        var catalogItem = await _catalogItemRepository.GetOneAsync(message.CatalogItemId);
        if (catalogItem == null)
        {
            throw new UnknownItemException(message.CatalogItemId);
        }

        var inventoryItem = await _inventoryItemRepository.GetOneAsync(x => x.UserId == message.UserId && x.CatalogItemId == message.CatalogItemId);

        if (inventoryItem is null)
        {
            inventoryItem = new InventoryItem
            {
                CatalogItemId = message.CatalogItemId,
                UserId = message.UserId,
                Quantity = message.Quantity,
                AcquireDate = DateTimeOffset.UtcNow
            };

            inventoryItem.MessageIds.Add(context.MessageId.Value);

            await _inventoryItemRepository.CreateAsync(inventoryItem);
        }
        else
        {
            if (inventoryItem.MessageIds.Contains(context.MessageId.Value))
            {
                await context.Publish(new InventoryItemsGranted(message.CorrelationId));
                return;
            }

            inventoryItem.Quantity += message.Quantity;
            inventoryItem.MessageIds.Add(context.MessageId.Value);
            await _inventoryItemRepository.UpdateAsync(inventoryItem);
        }

        var inventoryItemsGrantedTask = context.Publish(new InventoryItemsGranted(message.CorrelationId));
        var inventoryItemsUpdatedTask = context.Publish(new InventoryItemUpdated(inventoryItem.UserId, inventoryItem.CatalogItemId, inventoryItem.Quantity));
        await Task.WhenAll(inventoryItemsGrantedTask, inventoryItemsUpdatedTask);
    }
}
