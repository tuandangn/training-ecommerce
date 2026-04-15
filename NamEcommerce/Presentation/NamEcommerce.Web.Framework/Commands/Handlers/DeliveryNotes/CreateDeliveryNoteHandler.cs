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
        var selectedItems = request.Model.Items.Where(i => i.Selected && i.Quantity > 0).ToList();
        if (!selectedItems.Any())
            return false;

        var dto = new CreateDeliveryNoteAppDto
        {
            OrderId = request.Model.OrderId,
            ShippingAddress = request.Model.ShippingAddress,
            ShowPrice = request.Model.ShowPrice,
            Note = request.Model.Note,
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
