using NamEcommerce.Web.Contracts.Models.Common;

namespace NamEcommerce.Web.Contracts.Models.Catalog;

[Serializable]
public sealed class CategoryListModel
{
    public string? Keywords { get; set; }
    public BreadcrumbOptions BreadcrumbOpts { get; set; }

    public required IPagedDataModel<ItemModel> Data { get; init; }

    [Serializable]
    public sealed record ItemModel(Guid Id)
    {
        public required string Name { get; init; }
        public string? Breadcrumb { get; init; }
        public int DisplayOrder { get; init; }
        public required Guid? ParentId { get; set; }
    }
}
