using System;

namespace Play.Inventory.Contracts;

public record GrantItems(Guid UserId, Guid CatalogItemId, int Quantity, Guid CorrelationId);
