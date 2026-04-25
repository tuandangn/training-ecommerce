using NamEcommerce.Application.Contracts.Dtos.Common;

namespace NamEcommerce.Application.Contracts.Dtos.Catalog;

[Serializable]
public abstract record BaseProductAppDto
{
    public required string Name { get; init; }
    public required string? ShortDesc { get; init; }
    public Guid? UnitMeasurementId { get; set; }
    public IEnumerable<ProductCategoryAppDto> Categories { get; set; } = [];
    public IEnumerable<ProductVendorAppDto> Vendors { get; set; } = [];
    public IEnumerable<Guid> Pictures { get; set; } = [];

    public virtual (bool valid, string? errorMessage) Validate()
    {
        if (string.IsNullOrEmpty(Name))
            return (false, "Error.ProductNameRequired");

        return (true, string.Empty);
    }
}

[Serializable]
public sealed record ProductAppDto(Guid Id) : BaseProductAppDto
{
    public decimal UnitPrice { get; set; }
    public decimal CostPrice { get; set; }
}

[Serializable]
public sealed record CreateProductAppDto : BaseProductAppDto
{
    public FileInfoAppDto? ImageFile { get; set; }

    public decimal? UnitPrice { get; set; }
    public decimal? CostPrice { get; set; }
    public IEnumerable<ProductStockAppDto> ProductStocks { get; set; } = [];

    public override (bool valid, string? errorMessage) Validate()
    {
        if (UnitPrice.HasValue && UnitPrice < 0)
            return (false, "Error.ProductUnitPriceCannotBeNegative");

        if (CostPrice.HasValue && CostPrice < 0)
            return (false, "Error.ProductCostPriceCannotBeNegative");

        foreach (var productStock in ProductStocks)
        {
            if (productStock.Quantity <= 0)
                return (false, "Error.QuantityMustBePositive");
        }

        return base.Validate();
    }
}
[Serializable]
public sealed record CreateProductResultAppDto
{
    public required bool Success { get; init; }
    public Guid? CreatedId { get; set; }
    public string? ErrorMessage { get; set; }
}

[Serializable]
public sealed record UpdateProductAppDto(Guid Id) : BaseProductAppDto
{
    public FileInfoAppDto? ImageFile { get; set; }

    public decimal? NewUnitPrice { get; set; }
    public string? ChangePriceReason { get; set; }

    public override (bool valid, string? errorMessage) Validate()
    {
        if (NewUnitPrice.HasValue && NewUnitPrice < 0)
            return (false, "Error.ProductUnitPriceCannotBeNegative");

        return base.Validate();
    }
}
[Serializable]
public sealed record UpdateProductResultAppDto
{
    public required bool Success { get; init; }
    public Guid UpdatedId { get; set; }
    public string? ErrorMessage { get; set; }
}

[Serializable]
public sealed record DeleteProductAppDto(Guid Id);

[Serializable]
public sealed record DeleteProductResultAppDto
{
    public required bool Success { get; init; }
    public string? ErrorMessage { get; set; }
}

[Serializable]
public sealed record ProductCategoryAppDto(Guid CategoryId, int DisplayOrder);

[Serializable]
public sealed record ProductVendorAppDto(Guid VendorId, int DisplayOrder);

[Serializable]
public sealed record ProductStockAppDto(Guid? WarehouseId, decimal Quantity);
