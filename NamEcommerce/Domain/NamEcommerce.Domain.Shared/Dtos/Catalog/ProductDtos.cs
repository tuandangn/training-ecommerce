using NamEcommerce.Domain.Shared.Exceptions.Catalog;

namespace NamEcommerce.Domain.Shared.Dtos.Catalog;

[Serializable]
public abstract record BaseProductDto
{
    public required string Name { get; init; }
    public required string? ShortDesc { get; init; }
    public IEnumerable<ProductCategoryDto> Categories { get; set; } = [];
    public IEnumerable<Guid> Pictures { get; set; } = [];
    public bool TrackInventory { get; set; }

    public virtual void Verify()
    {
        if (string.IsNullOrEmpty(Name))
            throw new ProductDataIsInvalidException("Product name is not empty");
    }
}

[Serializable]
public sealed record ProductDto(Guid Id) : BaseProductDto;

[Serializable]
public sealed record CreateProductDto : BaseProductDto;
[Serializable]
public sealed record CreateProductResultDto
{
    public required Guid CreatedId { get; init; }
}

[Serializable]
public sealed record UpdateProductDto(Guid Id) : BaseProductDto;
[Serializable]
public sealed record UpdateProductResultDto(Guid Id) : BaseProductDto;

[Serializable]
public sealed record ProductCategoryDto(Guid CategoryId, int DisplayOrder);
