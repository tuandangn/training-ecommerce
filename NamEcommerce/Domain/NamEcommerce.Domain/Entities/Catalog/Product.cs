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
        ArgumentException.ThrowIfNullOrEmpty(name);

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

    public bool TrackInventory { get; private set; }

    public DateTime CreatedOnUtc { get; }
    public DateTime? UpdatedOnUtc { get; internal set; }

    private readonly IList<ProductCategory> _productCategories = [];
    public IEnumerable<ProductCategory> ProductCategories
        => _productCategories.AsReadOnly();

    private readonly IList<ProductPicture> _productPictures = [];
    public IEnumerable<ProductPicture> ProductPictures
        => _productPictures.AsReadOnly();

    #region Methods

    internal void SetCostPrice(decimal costPrice)
    {
        if (costPrice < 0) throw new ArgumentOutOfRangeException(nameof(costPrice), "Cost price cannot be less than 0");
        CostPrice = costPrice;
    }

    internal void SetTrackInventory(bool trackInventory) => TrackInventory = trackInventory;

    internal async Task SetNameAsync(string name, INameExistCheckingService checker)
    {
        if (string.Equals(Name, name, StringComparison.Ordinal))
            return;

        ArgumentNullException.ThrowIfNull(checker);
        ArgumentException.ThrowIfNullOrEmpty(name);

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
            throw new ArgumentException($"Cannot found unit mesuarement with id {unitMeasurementId}", nameof(unitMeasurementId));

        UnitMeasurementId = unitMeasurementId;
    }

    internal void ClearProductCategories() => _productCategories.Clear();
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

    internal void ClearProductPictures() => _productPictures.Clear();
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

    #endregion
}

[Serializable]
public sealed record ProductCategory : AppEntity
{
    internal ProductCategory(Guid productId, Guid categoryId, int displayOrder) : base(Guid.Empty)
        => (ProductId, CategoryId, DisplayOrder) = (productId, categoryId, displayOrder);

    public Guid ProductId { get; init; }
    public Guid CategoryId { get; init; }

    public int DisplayOrder { get; set; }
}

[Serializable]
public sealed record ProductPicture : AppEntity
{
    public ProductPicture(Guid productId, Guid pictureId) : base(Guid.Empty)
        => (ProductId, PictureId) = (productId, pictureId);

    public Guid ProductId { get; init; }
    public Guid PictureId { get; init; }

    public int DisplayOrder { get; set; }
}
