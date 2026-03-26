using NamEcommerce.Domain.Entities.Media;
using NamEcommerce.Domain.Shared;
using NamEcommerce.Domain.Shared.Exceptions.Catalog;
using NamEcommerce.Domain.Shared.Helpers;
using NamEcommerce.Domain.Shared.Services;
using NamEcommerce.Domain.Shared.Services.Catalog;

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

    public DateTime CreatedOnUtc { get; set; }
    public DateTime? UpdatedOnUtc { get; set; }

    private readonly IList<ProductCategory> _productCategories = [];
    public IEnumerable<ProductCategory> ProductCategories
        => _productCategories.AsEnumerable();

    private readonly IList<ProductPicture> _productPictures = [];
    public IEnumerable<ProductPicture> ProductPictures
        => _productPictures.AsEnumerable();

    #region Methods

    internal async Task SetNameAsync(string name, IProductManager manager)
    {
        ArgumentNullException.ThrowIfNull(manager);
        ArgumentException.ThrowIfNullOrEmpty(name);

        if (string.Equals(Name, name, StringComparison.Ordinal))
            return;

        if (await manager.DoesNameExistAsync(name, Id).ConfigureAwait(false))
            throw new ProductNameExistsException(name);

        Name = name;
        NormalizedName = TextHelper.Normalize(name);
    }

    internal void ClearProductCategories() => _productCategories.Clear();
    internal async Task AddToCategoryAsync(Guid categoryId, int displayOrder, IEntityDataReader<Category> dataReader)
    {
        ArgumentNullException.ThrowIfNull(dataReader);

        if (ProductCategories.Any(pc => pc.CategoryId == categoryId))
            throw new ProductAlreadyInCategoryException(categoryId, Name);

        var category = await dataReader.GetByIdAsync(categoryId).ConfigureAwait(false);
        ArgumentNullException.ThrowIfNull(category);

        _productCategories.Add(new ProductCategory(default, Id, categoryId, displayOrder));
    }

    internal void ClearProductPictures() => _productPictures.Clear();
    internal async Task AddPictureAsync(Guid pictureId, IEntityDataReader<Picture> dataReader)
    {
        ArgumentNullException.ThrowIfNull(dataReader);

        if (ProductPictures.Any(pc => pc.PictureId == pictureId))
            return;

        var picture = await dataReader.GetByIdAsync(pictureId).ConfigureAwait(false);
        ArgumentNullException.ThrowIfNull(picture);

        _productPictures.Add(new ProductPicture(default, Id, pictureId));
    }

    #endregion
}
