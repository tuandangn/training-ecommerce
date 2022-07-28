using System;

namespace NamEcommerce.Domain.Entities.Catalog;

[Serializable]
public sealed record Category : AppEntity
{
    internal Category(int id, string name) : base(id)
        => Name = name;

    public string Name { get; init; }

    public int DisplayOrder { get; set; }
}
