using System;
using System.Runtime.Serialization;

namespace Play.Inventory.Service.Exceptions;

[Serializable]
internal class UnknownItemException : Exception
{
    public Guid CatalogItemId { get; set; }

    public UnknownItemException(Guid catalogItemId) : base($"Unknown item'{catalogItemId}'")
    {
        this.CatalogItemId = catalogItemId;
    }
}