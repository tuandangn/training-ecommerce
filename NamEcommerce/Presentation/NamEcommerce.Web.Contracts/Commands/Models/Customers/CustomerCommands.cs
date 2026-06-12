using NamEcommerce.Web.Contracts.Models.Customers;

namespace NamEcommerce.Web.Contracts.Commands.Models.Customers;

[Serializable]
public sealed class CreateCustomerCommand : ICommand<CreateCustomerResultModel>
{
    public required string FullName { get; init; }
    public required string PhoneNumber { get; init; }
    public required string Address { get; init; }
    public string? Email { get; set; }
    public string? Note { get; set; }
    /// <summary>Công nợ ban đầu — nếu > 0 sẽ tạo một phiếu CustomerDebt số dư đầu kỳ.</summary>
    public decimal? InitialDebt { get; set; }
}

[Serializable]
public sealed class UpdateCustomerCommand : ICommand<UpdateCustomerResultModel>
{
    public required Guid Id { get; init; }
    public required string FullName { get; init; }
    public required string PhoneNumber { get; init; }
    public required string Address { get; init; }
    public string? Email { get; set; }
    public string? Note { get; set; }
}

[Serializable]
public sealed class DeleteCustomerCommand : ICommand<DeleteCustomerResultModel>
{
    public required Guid Id { get; init; }
}
