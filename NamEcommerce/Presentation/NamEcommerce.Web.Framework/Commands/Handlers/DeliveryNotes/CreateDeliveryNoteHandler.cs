using MediatR;
using NamEcommerce.Application.Contracts.DeliveryNotes;
using NamEcommerce.Application.Contracts.Dtos.DeliveryNotes;
using NamEcommerce.Web.Contracts.Commands.Models.DeliveryNotes;

namespace NamEcommerce.Web.Framework.Commands.Handlers.DeliveryNotes;

public sealed class CreateDeliveryNoteHandler : IRequestHandler<CreateDeliveryNoteCommand, bool>
{
    private readonly IDeliveryNoteAppService _deliveryNoteAppService;

    public CreateDeliveryNoteHandler(IDeliveryNoteAppService deliveryNoteAppService)
    {
        _deliveryNoteAppService = deliveryNoteAppService;
    }

    public async Task<bool> Handle(CreateDeliveryNoteCommand request, CancellationToken cancellationToken)
    {
        var selectedItems = request.Items.Where(i => i.Quantity > 0).ToList();
        if (!selectedItems.Any())
            return false;

        var dto = new CreateDeliveryNoteAppDto
        {
            OrderId = request.OrderId,
            ShippingAddress = request.ShippingAddress,
            WarehouseId = request.WarehouseId,
            ShowPrice = request.ShowPrice,
            Note = request.Note,
            AmountToCollect = request.AmountToCollect,
            Surcharge = request.Surcharge,
            SurchargeReason = request.SurchargeReason,
            Items = selectedItems.Select(i => new CreateDeliveryNoteItemAppDto
            {
                OrderItemId = i.OrderItemId,
                Quantity = i.Quantity
            }).ToList()
        };

        try
        {
            await _deliveryNoteAppService.CreateFromOrderAsync(dto).ConfigureAwait(false);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
