﻿namespace NamEcommerce.Domain.Shared.Dtos.Catalog;

[Serializable]
public sealed record CategoryDto
{
    public CategoryDto(int id, string name)
        => (Id, Name) = (id, name);

    public int Id { get; init; }
    public string Name { get; set; }
    public int DisplayOrder { get; set; }

    public int? ParentId { get; set; }
    public int OnParentDisplayOrder { get; set; }
}
