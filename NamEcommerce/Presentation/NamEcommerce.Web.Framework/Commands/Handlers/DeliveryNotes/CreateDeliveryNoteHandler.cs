using MediatR;
using NamEcommerce.Application.Contracts.DeliveryNotes;
using NamEcommerce.Application.Contracts.Dtos.DeliveryNotes;
using NamEcommerce.Web.Contracts.Commands.Models.DeliveryNotes;
using NamEcommerce.Web.Contracts.Models.Common;

namespace NamEcommerce.Web.Framework.Commands.Handlers.DeliveryNotes;

public sealed class CreateDeliveryNoteHandler : IRequestHandler<CreateDeliveryNoteCommand, CreateDeliveryNoteResultModel>
{
    private readonly IDeliveryNoteAppService _deliveryNoteAppService;

    public CreateDeliveryNoteHandler(IDeliveryNoteAppService deliveryNoteAppService)
    {
        _deliveryNoteAppService = deliveryNoteAppService;
    }

    public async Task<CreateDeliveryNoteResultModel> Handle(CreateDeliveryNoteCommand request, CancellationToken cancellationToken)
    {
        var selectedItems = request.Items.Where(i => i.Quantity > 0).ToList();
        if (!selectedItems.Any())
        {
            return new CreateDeliveryNoteResultModel
            {
                Success = false,
                ErrorMessage = "Error.DeliveryNoteItemsRequired"
            };
        }

        var dto = new CreateDeliveryNoteAppDto
        {
            OrderId = request.OrderId,
            ShippingAddress = request.ShippingAddress,
            ShippingPhoneNumber = request.ShippingPhoneNumber,
            WarehouseId = request.WarehouseId,
            ShowPrice = request.ShowPrice,
            Note = request.Note,
            AmountToCollect = request.AmountToCollect,
            Surcharge = request.Surcharge,
            SurchargeReason = request.SurchargeReason,
            Items = selectedItems.Select(i => new CreateDeliveryNoteItemAppDto
            {
                OrderItemId = i.OrderItemId,
                WarehouseId = i.WarehouseId == Guid.Empty ? (request.WarehouseId ?? Guid.Empty) : i.WarehouseId,
                Quantity = i.Quantity
            }).ToList()
        };
        var result = await _deliveryNoteAppService.CreateFromOrderAsync(dto).ConfigureAwait(false);

        return new CreateDeliveryNoteResultModel
        {
            Success = result.Success,
            ErrorMessage = result.ErrorMessage,
            CreatedId = result.CreatedId
        };
    }
}

public sealed class UpdateDeliveryNoteShippingHandler(IDeliveryNoteAppService deliveryNoteAppService)
    : IRequestHandler<UpdateDeliveryNoteShippingCommand, CommonActionResultModel>
{
    public async Task<CommonActionResultModel> Handle(UpdateDeliveryNoteShippingCommand request, CancellationToken cancellationToken)
    {
        var result = await deliveryNoteAppService.UpdateShippingAsync(new UpdateDeliveryNoteShippingAppDto
        {
            DeliveryNoteId = request.DeliveryNoteId,
            ShippingAddress = request.ShippingAddress ?? string.Empty,
            ShippingPhoneNumber = request.ShippingPhoneNumber
        }).ConfigureAwait(false);

        return new CommonActionResultModel
        {
            Success = result.Success,
            ErrorMessage = result.ErrorMessage
        };
    }
}
