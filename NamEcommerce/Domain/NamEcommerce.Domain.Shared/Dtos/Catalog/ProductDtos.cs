using NamEcommerce.Domain.Shared.Exceptions.Catalog;

namespace NamEcommerce.Domain.Shared.Dtos.Catalog;

[Serializable]
public abstract record BaseProductDto
{
    public required string Name { get; init; }
    public required string? ShortDesc { get; init; }
    public Guid? UnitMeasurementId { get; set; }
    public IEnumerable<ProductCategoryDto> Categories { get; set; } = [];
    public IEnumerable<ProductVendorDto> Vendors { get; set; } = [];
    public IEnumerable<Guid> Pictures { get; set; } = [];

    public virtual void Verify()
    {
        if (string.IsNullOrEmpty(Name))
            throw new ProductDataIsInvalidException("Error.ProductNameRequired");
    }
}

[Serializable]
public sealed record ProductDto(Guid Id) : BaseProductDto
{
    public decimal CostPrice { get; set; }
    public decimal UnitPrice { get; set; }
}

[Serializable]
public sealed record CreateProductDto : BaseProductDto;
[Serializable]
public sealed record CreateProductResultDto
{
    public required Guid CreatedId { get; init; }
}

[Serializable]
public sealed record UpdateProductDto(Guid Id) : BaseProductDto
{
}

[Serializable]
public sealed record UpdateProductResultDto(Guid Id) : BaseProductDto;

[Serializable]
public sealed record UpdateProductPriceDto(Guid Id)
{
    public decimal UnitCost { get; set; }
    public decimal UnitPrice { get; set; }
    public string? ChangePriceReason { get; set; }

    public void Verify()
    {
        if (UnitCost < 0)
            throw new ProductCostPriceCannotBeNegativeException();
        if (UnitPrice < 0)
            throw new ProductUnitPriceCannotBeNegativeException();
    }
}
[Serializable]
public sealed record UpdateProductPriceResultDto
{
    public required bool Success { get; init; }
    public string? ErrorMessage { get; set; }
    public Guid UpdatedId { get; set; }
}

[Serializable]
public sealed record ProductCategoryDto(Guid CategoryId, int DisplayOrder);

[Serializable]
public sealed record ProductVendorDto(Guid VendorId, int DisplayOrder);

[Serializable]
public sealed record ProductPriceHistoryDto
{
    public Guid Id { get; init; }
    public decimal OldPrice { get; init; }
    public decimal NewPrice { get; init; }
    public decimal OldCostPrice { get; init; }
    public decimal NewCostPrice { get; init; }
    public string? Note { get; init; }
    public DateTime CreatedOnUtc { get; init; }
}