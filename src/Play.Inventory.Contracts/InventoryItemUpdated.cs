using System;

namespace Play.Inventory.Contracts;

public record InventoryItemUpdated(Guid UserId, Guid CatalogItemId, int NewTotalQuantity
);
