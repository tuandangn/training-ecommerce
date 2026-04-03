using MediatR;
using NamEcommerce.Web.Contracts.Models.Customers;

namespace NamEcommerce.Web.Contracts.Commands.Models.Customers;

[Serializable]
public sealed class CreateCustomerCommand : IRequest<CreateCustomerResultModel>
{
    public required string FullName { get; init; }
    public required string PhoneNumber { get; init; }
    public string? Email { get; init; }
    public string? Address { get; init; }
    public string? Note { get; init; }
}

[Serializable]
public sealed class UpdateCustomerCommand : IRequest<UpdateCustomerResultModel>
{
    public required Guid Id { get; init; }
    public required string FullName { get; init; }
    public required string PhoneNumber { get; init; }
    public string? Email { get; init; }
    public string? Address { get; init; }
    public string? Note { get; init; }
}

[Serializable]
public sealed class DeleteCustomerCommand : IRequest<DeleteCustomerResultModel>
{
    public required Guid Id { get; init; }
}
