using System;

namespace Play.Inventory.Service.Dtos;

public record GrantItemDto(Guid UserId, Guid CatalogItemId, int Quantity);
