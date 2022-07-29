using NamEcommerce.Domain.Entities.Media;
using NamEcommerce.Domain.Shared.Exceptions.Catalog;

namespace NamEcommerce.Domain.Entities.Catalog;

[Serializable]
public record Product : AppEntity
{
    internal Product(int id, string name, decimal price, string shortDesc)
        : this(id, name, price, shortDesc, Array.Empty<ProductCategory>(), Array.Empty<ProductPicture>())
    { }

    internal Product(int id, string name, decimal price, string shortDesc,
        IList<ProductCategory> productCategories, IList<ProductPicture> productPictures) : base(id)
        => (Name, Price, ShortDesc, _productCategories, _productPictures) 
        = (name, price, shortDesc, productCategories, productPictures);

    public string Name { get; init; }
    public string ShortDesc { get; init; }
    public string? FullDesc { get; set; }

    public decimal Price { get; init; }
    public decimal? Discount { get; set; }
    public decimal? Tax { get; set; }

    public DateTime CreatedOnUtc { get; init; }

    private IList<ProductCategory> _productCategories;
    public IEnumerable<ProductCategory> ProductCategories
        => _productCategories.AsEnumerable();

    private IList<ProductPicture> _productPictures;
    public IEnumerable<ProductPicture> ProductPictures
        => _productPictures.AsEnumerable();

    #region Methods

    internal void SetProductCategories(IList<ProductCategory> productCategories)
    {
        if (productCategories is null)
            throw new ArgumentNullException(nameof(productCategories));

        _productCategories = productCategories;
    }
    internal void AddToCategory(int categoryId, int displayOrder)
    {
        if (ProductCategories.Any(pc => pc.CategoryId == categoryId))
            throw new ProductAlreadyInCategoryException(categoryId, Name);

        _productCategories.Add(new ProductCategory(default, Id, categoryId)
        {
            DisplayOrder = displayOrder
        });
    }
    internal void SetProductPictures(IList<ProductPicture> productPictues)
    {
        if (productPictues is null)
            throw new ArgumentNullException(nameof(productPictues));

        _productPictures = productPictues;
    }
    internal void AddPicture(int pictureId, int displayOrder)
    {
        _productPictures.Add(new ProductPicture(default, Id, pictureId)
        {
            DisplayOrder = displayOrder
        });
    }

    #endregion
}
