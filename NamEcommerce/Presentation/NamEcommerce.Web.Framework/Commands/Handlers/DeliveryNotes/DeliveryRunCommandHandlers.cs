using MediatR;
using NamEcommerce.Application.Contracts.DeliveryNotes;
using NamEcommerce.Application.Contracts.Dtos.Common;
using NamEcommerce.Application.Contracts.Dtos.DeliveryNotes;
using NamEcommerce.Web.Contracts.Commands.Models.DeliveryNotes;
using NamEcommerce.Web.Contracts.Models.Common;

namespace NamEcommerce.Web.Framework.Commands.Handlers.DeliveryNotes;

public sealed class CreateDeliveryRunHandler(IDeliveryRunAppService deliveryRunAppService)
    : IRequestHandler<CreateDeliveryRunCommand, CreateDeliveryRunResultModel>
{
    public async Task<CreateDeliveryRunResultModel> Handle(CreateDeliveryRunCommand request, CancellationToken cancellationToken)
    {
        var result = await deliveryRunAppService.CreateAsync(new CreateDeliveryRunAppDto
        {
            AssignedDeliveryUserId = request.AssignedDeliveryUserId,
            DeliveryNoteIds = request.DeliveryNoteIds,
            Note = request.Note
        }).ConfigureAwait(false);

        return new CreateDeliveryRunResultModel
        {
            Success = result.Success,
            ErrorMessage = result.ErrorMessage,
            SuccessMessage = result.Success ? "Msg.SaveSuccess" : null,
            CreatedId = result.CreatedId
        };
    }
}

public sealed class IssuePaperManifestDeliveryRunHandler(IDeliveryRunAppService deliveryRunAppService)
    : IRequestHandler<IssuePaperManifestDeliveryRunCommand, CommonActionResultModel>
{
    public async Task<CommonActionResultModel> Handle(IssuePaperManifestDeliveryRunCommand request, CancellationToken cancellationToken)
        => MapResult(await deliveryRunAppService.IssuePaperManifestAsync(request.Id).ConfigureAwait(false));

    private static CommonActionResultModel MapResult(CommonActionResultDto result)
        => DeliveryRunActionResultMapper.Map(result);
}

public sealed class AcknowledgeDriverCacheDeliveryRunHandler(IDeliveryRunAppService deliveryRunAppService)
    : IRequestHandler<AcknowledgeDriverCacheDeliveryRunCommand, CommonActionResultModel>
{
    public async Task<CommonActionResultModel> Handle(AcknowledgeDriverCacheDeliveryRunCommand request, CancellationToken cancellationToken)
        => DeliveryRunActionResultMapper.Map(await deliveryRunAppService.AcknowledgeDriverCacheAsync(request.Id, request.DeviceId).ConfigureAwait(false));
}

public sealed class HandOverDeliveryRunHandler(IDeliveryRunAppService deliveryRunAppService)
    : IRequestHandler<HandOverDeliveryRunCommand, CommonActionResultModel>
{
    public async Task<CommonActionResultModel> Handle(HandOverDeliveryRunCommand request, CancellationToken cancellationToken)
        => DeliveryRunActionResultMapper.Map(await deliveryRunAppService.HandOverAsync(request.Id).ConfigureAwait(false));
}

public sealed class CloseDeliveryRunHandler(IDeliveryRunAppService deliveryRunAppService)
    : IRequestHandler<CloseDeliveryRunCommand, CommonActionResultModel>
{
    public async Task<CommonActionResultModel> Handle(CloseDeliveryRunCommand request, CancellationToken cancellationToken)
        => DeliveryRunActionResultMapper.Map(await deliveryRunAppService.CloseAsync(request.Id).ConfigureAwait(false));
}

public sealed class ConfirmDeliveryRunCashHandoverHandler(IDeliveryRunAppService deliveryRunAppService)
    : IRequestHandler<ConfirmDeliveryRunCashHandoverCommand, CommonActionResultModel>
{
    public async Task<CommonActionResultModel> Handle(ConfirmDeliveryRunCashHandoverCommand request, CancellationToken cancellationToken)
        => DeliveryRunActionResultMapper.Map(await deliveryRunAppService.ConfirmCashHandoverAsync(new ConfirmDeliveryRunCashHandoverAppDto
        {
            DeliveryRunId = request.Id,
            Amount = request.Amount,
            Note = request.Note
        }).ConfigureAwait(false));
}

public sealed class UpdateDeliveryRunDeliveredNoteCashCollectedHandler(IDeliveryRunAppService deliveryRunAppService)
    : IRequestHandler<UpdateDeliveryRunDeliveredNoteCashCollectedCommand, CommonActionResultModel>
{
    public async Task<CommonActionResultModel> Handle(UpdateDeliveryRunDeliveredNoteCashCollectedCommand request, CancellationToken cancellationToken)
        => DeliveryRunActionResultMapper.Map(await deliveryRunAppService.UpdateDeliveredNoteCashCollectedAsync(new UpdateDeliveryRunDeliveredNoteCashCollectedAppDto
        {
            DeliveryRunId = request.Id,
            DeliveryNoteId = request.DeliveryNoteId,
            CashCollectedAmount = request.CashCollectedAmount
        }).ConfigureAwait(false));
}

public sealed class CancelDeliveryRunHandler(IDeliveryRunAppService deliveryRunAppService)
    : IRequestHandler<CancelDeliveryRunCommand, CommonActionResultModel>
{
    public async Task<CommonActionResultModel> Handle(CancelDeliveryRunCommand request, CancellationToken cancellationToken)
        => DeliveryRunActionResultMapper.Map(await deliveryRunAppService.CancelAsync(request.Id).ConfigureAwait(false));
}

public sealed class ConfirmDeliveryRunWarehousePickHandler(IDeliveryRunAppService deliveryRunAppService)
    : IRequestHandler<ConfirmDeliveryRunWarehousePickCommand, CommonActionResultModel>
{
    public async Task<CommonActionResultModel> Handle(ConfirmDeliveryRunWarehousePickCommand request, CancellationToken cancellationToken)
        => DeliveryRunActionResultMapper.Map(await deliveryRunAppService.ConfirmWarehousePickAsync(request.Id, request.WarehouseId).ConfigureAwait(false));
}

file static class DeliveryRunActionResultMapper
{
    public static CommonActionResultModel Map(CommonActionResultDto result)
        => new()
        {
            Success = result.Success,
            ErrorMessage = result.ErrorMessage,
            SuccessMessage = result.Success ? "Msg.SaveSuccess" : null
        };
}
