using NamEcommerce.Web.Contracts.Models.Catalog;
using NamEcommerce.Web.Models.Catalog;

namespace NamEcommerce.Web.Services.Catalog;

public interface ICategoryModelFactory
{
    Task<CategoryListModel> PrepareCategoryListModel(CategoryListSearchModel searchModel);
    Task<CreateCategoryModel> PrepareCreateCategoryModel(CreateCategoryModel? model = null);
    Task<EditCategoryModel?> PrepareEditCategoryModel(Guid id, EditCategoryModel? model = null);
}