using NamEcommerce.Admin.Contracts.Models.Common;

namespace NamEcommerce.Admin.Contracts.Models.Catalog;

[Serializable]
public sealed class CategoryListModel
{
    public IEnumerable<CategoryModel> Categories { get; set; }
        = Enumerable.Empty<CategoryModel>();
    public PageInfoModel? PagerInfo { get; set; }
}
