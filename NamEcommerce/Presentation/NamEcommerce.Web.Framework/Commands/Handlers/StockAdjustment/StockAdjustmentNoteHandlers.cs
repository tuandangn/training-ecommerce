using MediatR;
using NamEcommerce.Application.Contracts.Dtos.StockAdjustment;
using NamEcommerce.Application.Contracts.StockAdjustment;
using NamEcommerce.Web.Contracts.Commands.StockAdjustment;
using NamEcommerce.Web.Contracts.Models.StockAdjustment;
using NamEcommerce.Web.Framework.Services;

namespace NamEcommerce.Web.Framework.Commands.Handlers.StockAdjustment;

public sealed class CreateStockAdjustmentNoteHandler(
    IStockAdjustmentNoteAppService appService,
    ICurrentUserService currentUserService)
    : IRequestHandler<CreateStockAdjustmentNoteCommand, CreateStockAdjustmentNoteResultModel>
{
    public async Task<CreateStockAdjustmentNoteResultModel> Handle(
        CreateStockAdjustmentNoteCommand request, CancellationToken cancellationToken)
    {
        var dto = new CreateStockAdjustmentNoteAppDto
        {
            WarehouseId = request.WarehouseId,
            Note = request.Note,
            Items = request.Items.Select(i => new CreateStockAdjustmentNoteItemAppDto
            {
                ProductId = i.ProductId,
                ProductName = i.ProductName,
                SystemQuantity = i.SystemQuantity,
                PhysicalQuantity = i.PhysicalQuantity
            }).ToList()
        };

        var result = await appService.CreateAsync(dto, currentUserService.GetCurrentUserId()).ConfigureAwait(false);
        return new CreateStockAdjustmentNoteResultModel
        {
            Success = result.Success,
            ErrorMessage = result.ErrorMessage,
            CreatedId = result.CreatedId
        };
    }
}

public sealed class ApproveStockAdjustmentNoteHandler(IStockAdjustmentNoteAppService appService)
    : IRequestHandler<ApproveStockAdjustmentNoteCommand, StockAdjustmentNoteActionResultModel>
{
    public async Task<StockAdjustmentNoteActionResultModel> Handle(
        ApproveStockAdjustmentNoteCommand request, CancellationToken cancellationToken)
    {
        var (success, error) = await appService.ApproveAsync(request.Id).ConfigureAwait(false);
        return new StockAdjustmentNoteActionResultModel { Success = success, ErrorMessage = error };
    }
}

public sealed class CancelStockAdjustmentNoteHandler(IStockAdjustmentNoteAppService appService)
    : IRequestHandler<CancelStockAdjustmentNoteCommand, StockAdjustmentNoteActionResultModel>
{
    public async Task<StockAdjustmentNoteActionResultModel> Handle(
        CancelStockAdjustmentNoteCommand request, CancellationToken cancellationToken)
    {
        var (success, error) = await appService.CancelAsync(request.Id).ConfigureAwait(false);
        return new StockAdjustmentNoteActionResultModel { Success = success, ErrorMessage = error };
    }
}
