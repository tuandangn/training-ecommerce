using NamEcommerce.Web.Contracts.Common;

namespace NamEcommerce.Web.Contracts.Models.Catalog;

[Serializable]
public sealed class VendorListModel
{
    public string? Keywords { get; set; }

    public required IPagedDataModel<ItemModel> Data { get; init; }

    [Serializable]
    public sealed record ItemModel(Guid Id)
    {
        public required string Name { get; init; }
        public required string PhoneNumber { get; init; }
        public string? Address { get; set; }
        public int DisplayOrder { get; init; }
    }
}
