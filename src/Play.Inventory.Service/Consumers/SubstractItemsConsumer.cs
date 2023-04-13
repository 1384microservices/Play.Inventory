using System.Threading.Tasks;
using MassTransit;
using Play.Common;
using Play.Inventory.Contracts;
using Play.Inventory.Service.Entities;
using Play.Inventory.Service.Exceptions;

namespace Play.Inventory.Service.Consumers;

public class SubstractItemsConsumer : IConsumer<SubstractItems>
{
    private readonly IRepository<InventoryItem> _inventoryItemRepository;

    private readonly IRepository<CatalogItem> _catalogItemRepository;

    public SubstractItemsConsumer(IRepository<InventoryItem> inventoryItemRepository, IRepository<CatalogItem> catalogItemRepository)
    {
        _inventoryItemRepository = inventoryItemRepository;
        _catalogItemRepository = catalogItemRepository;
    }

    public async Task Consume(ConsumeContext<SubstractItems> context)
    {
        var message = context.Message;

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
