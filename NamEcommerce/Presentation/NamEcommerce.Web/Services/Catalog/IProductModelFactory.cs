using NamEcommerce.Web.Contracts.Models.Catalog;
using NamEcommerce.Web.Models.Catalog;

namespace NamEcommerce.Web.Services.Catalog;

public interface IProductModelFactory
{
    Task<ProductListModel> PrepareProductListModel(ProductSearchModel searchModel);
    Task<CreateProductModel> PrepareCreateProductModel(CreateProductModel? model = null);
    Task<EditProductModel?> PrepareEditProductModel(Guid id, EditProductModel? model = null);
}
