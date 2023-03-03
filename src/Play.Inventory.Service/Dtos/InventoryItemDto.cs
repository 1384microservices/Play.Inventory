using System;

namespace Play.Inventory.Service.Dtos;

public record InventoryItemDto(Guid CatalogItemId, string Name, string Description, int Quantity, DateTimeOffset AcquireDate);