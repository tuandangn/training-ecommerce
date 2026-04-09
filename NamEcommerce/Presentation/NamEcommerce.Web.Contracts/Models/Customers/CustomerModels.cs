using NamEcommerce.Web.Contracts.Models.Common;

namespace NamEcommerce.Web.Contracts.Models.Customers;

[Serializable]
public sealed record CustomerModel
{
    public required Guid Id { get; init; }
    public required string FullName { get; init; }
    public required string PhoneNumber { get; init; }
    public string? Email { get; init; }
    public string? Address { get; init; }
    public string? Note { get; init; }
    public DateTime CreatedOnUtc { get; init; }
}

[Serializable]
public sealed class CustomerListModel
{
    public string? Keywords { get; set; }
    public required IPagedDataModel<CustomerItemModel> Data { get; init; }

    [Serializable]
    public sealed record CustomerItemModel(Guid Id)
    {
        public required string FullName { get; init; }
        public required string PhoneNumber { get; init; }
        public required string Address { get; set; }
    }
}

[Serializable]
public sealed record CreateCustomerResultModel
{
    public required bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public Guid CreatedId { get; set; }
}

[Serializable]
public sealed record UpdateCustomerResultModel
{
    public required bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public Guid UpdatedId { get; set; }
}

[Serializable]
public sealed record DeleteCustomerResultModel
{
    public required bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public Guid DeletedId { get; set; }
}
