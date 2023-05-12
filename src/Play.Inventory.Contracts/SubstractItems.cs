using System;

namespace Play.Inventory.Contracts;

public record SubstractItems(Guid UserId, Guid CatalogItemId, int Quantity, Guid CorrelationId);
