using NamEcommerce.Application.Contracts.Dtos.Common;

namespace NamEcommerce.Application.Contracts.Dtos.Catalog;

[Serializable]
public abstract record BaseProductAppDto
{
    public required string Name { get; init; }
    public required string? ShortDesc { get; init; }
    public Guid? UnitMeasurementId { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal CostPrice { get; set; }
    public IEnumerable<ProductCategoryAppDto> Categories { get; set; } = [];
    public IEnumerable<ProductVendorAppDto> Vendors { get; set; } = [];
    public IEnumerable<Guid> Pictures { get; set; } = [];

    public (bool valid, string? errorMessage) Validate()
    {
        if (string.IsNullOrEmpty(Name))
            return (false, "Error.ProductNameRequired");

        if (UnitPrice < 0)
            return (false, "Error.ProductUnitPriceCannotBeNegative");

        if (CostPrice < 0)
            return (false, "Error.ProductCostPriceCannotBeNegative");

        return (true, string.Empty);
    }
}

[Serializable]
public sealed record ProductAppDto(Guid Id) : BaseProductAppDto;

[Serializable]
public sealed record CreateProductAppDto : BaseProductAppDto
{
    public FileInfoAppDto? ImageFile { get; set; }
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
    public string? ChangePriceReason { get; set; }
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
