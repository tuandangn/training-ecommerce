using MediatR;
using NamEcommerce.Web.Contracts.Models.Returns;

namespace NamEcommerce.Web.Contracts.Queries.Models.Returns;

[Serializable]
public sealed class GetDeliveryNotesByCustomerQuery : IRequest<List<DeliveryNotePickerModel>>
{
    public required Guid CustomerId { get; init; }
}
