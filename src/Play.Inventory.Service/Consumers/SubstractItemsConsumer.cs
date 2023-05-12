using System.Threading.Tasks;
using MassTransit;
using Microsoft.Extensions.Logging;
using Play.Common;
using Play.Inventory.Contracts;
using Play.Inventory.Service.Entities;
using Play.Inventory.Service.Exceptions;

namespace Play.Inventory.Service.Consumers;

public class SubstractItemsConsumer : IConsumer<SubstractItems>
{
    private readonly IRepository<InventoryItem> _inventoryItemRepository;
    private readonly IRepository<CatalogItem> _catalogItemRepository;
    private readonly ILogger<SubstractItemsConsumer> _logger;

    public SubstractItemsConsumer(
        IRepository<InventoryItem> inventoryItemRepository, 
        IRepository<CatalogItem> catalogItemRepository, 
        ILogger<SubstractItemsConsumer> logger)
    {
        _inventoryItemRepository = inventoryItemRepository;
        _catalogItemRepository = catalogItemRepository;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<SubstractItems> context)
    {
        var message = context.Message;

        _logger.LogInformation(
            "Substract {Quantity} of item {CatalogItemId} for user {UserId} with {CorrelationId}", 
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

        if (inventoryItem is not null)
        {
            if (inventoryItem.MessageIds.Contains(context.MessageId.Value))
            {
                await context.Publish(new InventoryItemsSubstracted(message.CcorrelationId));
                return;
            }
            inventoryItem.Quantity -= message.Quantity;
            await _inventoryItemRepository.UpdateAsync(inventoryItem);
            await context.Publish(new InventoryItemUpdated(inventoryItem.UserId, inventoryItem.CatalogItemId, inventoryItem.Quantity));
        }

        await context.Publish(new InventoryItemsSubstracted(message.CcorrelationId));
        
    }
}
