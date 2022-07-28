using System;

namespace NamEcommerce.Domain.Entities.Catalog;

[Serializable]
public record Product : AppEntity
{
    internal Product(int id, string name) : base(id)
        => Name = name;

    public string Name { get; init; }

    public DateTime CreatedOnUtc { get; init; }
}
