using System;
using System.Collections.Generic;
using Play.Common;

namespace Play.Inventory.Service.Entities;

public class InventoryItem : IEntity
{
    public Guid Id { get; set; }
    public HashSet<Guid> MessageIds { get; set; } = new();
    public Guid UserId { get; set; }
    public Guid CatalogItemId { get; set; }
    public int Quantity { get; set; }
    public DateTimeOffset AcquireDate { get; set; }
}
