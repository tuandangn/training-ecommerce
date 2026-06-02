using MediatR;

namespace NamEcommerce.Web.Contracts.Queries.Models.Inventory;

public sealed class GetDeliveryNoteReturnWarehouseQuery : IRequest<string?>
{
    public Guid DeliveryNoteId { get; init; }
    public Guid DeliveryNoteWarehouseId { get; init; }
}
