using MediatR;
using NamEcommerce.Application.Contracts.Dtos.PurchaseOrders;
using NamEcommerce.Application.Contracts.PurchaseOrders;
using NamEcommerce.Web.Contracts.Commands.Models.PurchaseOrders;
using NamEcommerce.Web.Contracts.Models.Common;

namespace NamEcommerce.Web.Framework.Commands.Handlers.PurchaseOrders;

public sealed class ConfirmDirectShipDeliveryHandler(IDirectShipAppService directShipAppService)
    : IRequestHandler<ConfirmDirectShipDeliveryCommand, CommonActionResultModel>
{
    public async Task<CommonActionResultModel> Handle(ConfirmDirectShipDeliveryCommand request, CancellationToken cancellationToken)
    {
        var result = await directShipAppService.ConfirmDeliveryAsync(new ConfirmDirectShipDeliveryAppDto
        {
            DeliveryNoteId = request.DeliveryNoteId,
            ConfirmedAtUtc = request.ConfirmedAtUtc,
            Note = request.Note
        }).ConfigureAwait(false);

        return new CommonActionResultModel { Success = result.Success, ErrorMessage = result.ErrorMessage };
    }
}

public sealed class RejectDirectShipDeliveryHandler(IDirectShipAppService directShipAppService)
    : IRequestHandler<RejectDirectShipDeliveryCommand, CommonActionResultModel>
{
    public async Task<CommonActionResultModel> Handle(RejectDirectShipDeliveryCommand request, CancellationToken cancellationToken)
    {
        var result = await directShipAppService.RejectDeliveryAsync(new RejectDirectShipDeliveryAppDto
        {
            DeliveryNoteId = request.DeliveryNoteId,
            ReturnWarehouseId = request.ReturnWarehouseId,
            Reason = request.Reason
        }).ConfigureAwait(false);

        return new CommonActionResultModel { Success = result.Success, ErrorMessage = result.ErrorMessage };
    }
}

public sealed class UpdateDirectShipAddressHandler(IDirectShipAppService directShipAppService)
    : IRequestHandler<UpdateDirectShipAddressCommand, CommonActionResultModel>
{
    public async Task<CommonActionResultModel> Handle(UpdateDirectShipAddressCommand request, CancellationToken cancellationToken)
    {
        var result = await directShipAppService.UpdateDirectShipAddressAsync(new UpdateDirectShipAddressAppDto
        {
            AllocationId = request.AllocationId,
            NewAddress = request.NewAddress,
            NewContactName = request.NewContactName,
            NewContactPhone = request.NewContactPhone,
            Reason = request.Reason
        }).ConfigureAwait(false);

        return new CommonActionResultModel { Success = result.Success, ErrorMessage = result.ErrorMessage };
    }
}

public sealed class MarkAllocationAsDirectShipHandler(IDirectShipAppService directShipAppService)
    : IRequestHandler<MarkAllocationAsDirectShipCommand, CommonActionResultModel>
{
    public async Task<CommonActionResultModel> Handle(MarkAllocationAsDirectShipCommand request, CancellationToken cancellationToken)
    {
        var result = await directShipAppService.MarkAllocationAsDirectShipAsync(new MarkAllocationAsDirectShipAppDto
        {
            AllocationId = request.AllocationId,
            Address = request.Address,
            ContactName = request.ContactName,
            ContactPhone = request.ContactPhone,
            Priority = 0
        }).ConfigureAwait(false);

        return new CommonActionResultModel { Success = result.Success, ErrorMessage = result.ErrorMessage };
    }
}
