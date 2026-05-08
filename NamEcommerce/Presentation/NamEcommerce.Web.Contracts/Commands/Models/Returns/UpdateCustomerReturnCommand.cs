using MediatR;
using NamEcommerce.Web.Contracts.Models.Returns;

namespace NamEcommerce.Web.Contracts.Commands.Models.Returns;

[Serializable]
public sealed class UpdateCustomerReturnCommand : IRequest<UpdateCustomerReturnResultModel>
{
    public required Guid Id { get; init; }
    public string? Note { get; init; }
    public DateTime? ReturnDate { get; init; }
}
