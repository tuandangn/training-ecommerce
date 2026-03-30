using NamEcommerce.Web.Contracts.Models.Common;

namespace NamEcommerce.Web.Contracts.Models.Catalog;

[Serializable]
public sealed class ProductListModel
{
    public string? Keywords { get; set; }

    public required IPagedDataModel<ProductItemModel> Data { get; init; }

    [Serializable]
    public sealed record ProductItemModel(Guid Id)
    {
        public required string Name { get; init; }
        public string? ShortDesc { get; init; }
        public string? PictureUrl { get; set; }
        public Guid? CategoryId { get; set; }
        public string? CategoryBreadcrumb { get; set; }
    }
}
