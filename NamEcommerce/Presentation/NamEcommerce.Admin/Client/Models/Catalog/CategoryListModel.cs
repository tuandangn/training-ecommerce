using NamEcommerce.Admin.Client.Models.Common;

namespace NamEcommerce.Admin.Client.Models.Catalog;

[Serializable]
public sealed class CategoryListModel
{
    public IEnumerable<CategoryModel> Categories { get; set; }
        = Enumerable.Empty<CategoryModel>();
    public PageInfoModel? PagerInfo { get; set; }

    [Serializable]
    public record CategoryModel(Guid Id, string Name, CategoryModel? Parent);
}
