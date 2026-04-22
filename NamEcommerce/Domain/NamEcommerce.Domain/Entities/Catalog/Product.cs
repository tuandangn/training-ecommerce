using NamEcommerce.Domain.Entities.Media;
using NamEcommerce.Domain.Shared;
using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Domain.Shared.Exceptions.Catalog;
using NamEcommerce.Domain.Shared.Exceptions.Media;
using NamEcommerce.Domain.Shared.Helpers;

namespace NamEcommerce.Domain.Entities.Catalog;

[Serializable]
public record Product : AppAggregateEntity
{
    internal Product(Guid id, string name) : base(id)
    {
        if (string.IsNullOrEmpty(name))
            throw new ProductNameRequiredException();

        Name = name;
        NormalizedName = TextHelper.Normalize(Name);

        CreatedOnUtc = DateTime.UtcNow;
    }

    public string Name { get; private set; }
    internal string NormalizedName { get; private set; } = "";

    public string? ShortDesc
    {
        get;
        internal set
        {
            field = value;
            NormalizedShortDesc = TextHelper.Normalize(value);
        }
    }
    internal string NormalizedShortDesc { get; private set; } = "";

    public Guid? UnitMeasurementId { get; private set; }

    public decimal CostPrice { get; private set; }
    public decimal UnitPrice { get; private set; }

    public DateTime CreatedOnUtc { get; }
    public DateTime? UpdatedOnUtc { get; internal set; }

    private readonly IList<ProductCategory> _productCategories = [];
    public IEnumerable<ProductCategory> ProductCategories
        => _productCategories.AsReadOnly();

    private readonly IList<ProductVendor> _productVendors = [];
    public IEnumerable<ProductVendor> ProductVendors
        => _productVendors.AsReadOnly();

    private readonly IList<ProductPicture> _productPictures = [];
    public IEnumerable<ProductPicture> ProductPictures
        => _productPictures.AsReadOnly();

    #region Methods

    internal async Task SetNameAsync(string name, INameExistCheckingService checker)
    {
        if (string.Equals(Name, name, StringComparison.Ordinal))
            return;

        ArgumentNullException.ThrowIfNull(checker);
        if (string.IsNullOrEmpty(name))
            throw new ProductNameRequiredException();

        if (await checker.DoesNameExistAsync(name, Id).ConfigureAwait(false))
            throw new ProductNameExistsException(name);

        Name = name;
        NormalizedName = TextHelper.Normalize(name);
    }

    internal async Task SetUnitMeasurementAsync(Guid? unitMeasurementId, IGetByIdService<UnitMeasurement> byIdGetter)
    {
        if (UnitMeasurementId == unitMeasurementId)
            return;

        if (!unitMeasurementId.HasValue)
        {
            UnitMeasurementId = null;
            return;
        }

        ArgumentNullException.ThrowIfNull(byIdGetter);

        var unitMeassurement = await byIdGetter.GetByIdAsync(unitMeasurementId.Value).ConfigureAwait(false);
        if (unitMeassurement is null)
            throw new UnitMeasurementIsNotFoundException(unitMeasurementId.Value);

        UnitMeasurementId = unitMeasurementId;
    }

    internal void ClearCategories() => _productCategories.Clear();
    internal async Task AddToCategoryAsync(Guid categoryId, int displayOrder, IGetByIdService<Category> byIdGetter)
    {
        ArgumentNullException.ThrowIfNull(byIdGetter);

        if (ProductCategories.Any(pc => pc.CategoryId == categoryId))
            throw new ProductAlreadyInCategoryException(categoryId, Name);

        var category = await byIdGetter.GetByIdAsync(categoryId).ConfigureAwait(false);
        if (category is null)
            throw new CategoryIsNotFoundException(categoryId);

        _productCategories.Add(new ProductCategory(Id, categoryId, displayOrder));
    }
    internal void RemoveFromCategory(Guid categoryId)
    {
        var productCategory = _productCategories.FirstOrDefault(pc => pc.CategoryId == categoryId);
        if (productCategory is null)
            return;

        _productCategories.Remove(productCategory);
    }

    internal void ClearVendors() => _productVendors.Clear();
    internal async Task AddVendorAsync(Guid vendorId, int displayOrder, IGetByIdService<Vendor> byIdGetter)
    {
        ArgumentNullException.ThrowIfNull(byIdGetter);

        if (ProductVendors.Any(pv => pv.VendorId == vendorId))
            return;

        var vendor = await byIdGetter.GetByIdAsync(vendorId).ConfigureAwait(false);
        if (vendor is null)
            throw new VendorIsNotFoundException(vendorId);

        _productVendors.Add(new ProductVendor(Id, vendorId, displayOrder));
    }
    internal void RemoveVendor(Guid vendorId)
    {
        var productVendor = _productVendors.FirstOrDefault(pv => pv.VendorId == vendorId);
        if (productVendor is null)
            return;

        _productVendors.Remove(productVendor);
    }

    internal void ClearPictures() => _productPictures.Clear();
    internal async Task AddPictureAsync(Guid pictureId, IGetByIdService<Picture> byIdGetter)
    {
        ArgumentNullException.ThrowIfNull(byIdGetter);

        if (ProductPictures.Any(pc => pc.PictureId == pictureId))
            return;

        var picture = await byIdGetter.GetByIdAsync(pictureId).ConfigureAwait(false);
        if (picture is null)
            throw new PictureIsNotFoundException(pictureId);

        _productPictures.Add(new ProductPicture(Id, pictureId));
    }

    internal void UpdatePrice(decimal unitPrice, decimal costPrice)
    {
        if (unitPrice < 0)
            throw new ProductUnitPriceCannotBeNegativeException();

        if (costPrice < 0)
            throw new ProductCostPriceCannotBeNegativeException();

        if (unitPrice == UnitPrice && costPrice == CostPrice)
            return;

        UnitPrice = unitPrice;
        CostPrice = costPrice;
    }

    #endregion
}

[Serializable]
public sealed record ProductCategory : AppEntity
{
    internal ProductCategory(Guid productId, Guid categoryId, int displayOrder) : base(Guid.Empty)
    {
        ProductId = productId;
        CategoryId = categoryId;
        DisplayOrder = displayOrder;
    }

    public Guid ProductId { get; init; }
    public Guid CategoryId { get; init; }
    public int DisplayOrder { get; set; }
}

[Serializable]
public sealed record ProductVendor : AppEntity
{
    internal ProductVendor(Guid productId, Guid vendorId, int displayOrder) : base(Guid.Empty)
    {
        ProductId = productId;
        VendorId = vendorId;
        DisplayOrder = displayOrder;
    }

    public Guid ProductId { get; init; }
    public Guid VendorId { get; init; }
    public int DisplayOrder { get; set; }
}

[Serializable]
public sealed record ProductPicture : AppEntity
{
    internal ProductPicture(Guid productId, Guid pictureId) : base(Guid.Empty)
    {
        ProductId = productId;
        PictureId = pictureId;
    }

    public Guid ProductId { get; init; }
    public Guid PictureId { get; init; }
    public int DisplayOrder { get; set; }
} 