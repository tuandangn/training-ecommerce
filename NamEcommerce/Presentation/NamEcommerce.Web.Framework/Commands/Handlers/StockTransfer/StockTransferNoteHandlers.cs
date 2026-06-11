using MediatR;
using NamEcommerce.Application.Contracts.Dtos.StockTransfer;
using NamEcommerce.Application.Contracts.StockTransfer;
using NamEcommerce.Web.Contracts.Commands.StockTransfer;
using NamEcommerce.Web.Contracts.Models.StockTransfer;

namespace NamEcommerce.Web.Framework.Commands.Handlers.StockTransfer;

public sealed class CreateStockTransferNoteHandler(
    IStockTransferNoteAppService appService) : IRequestHandler<CreateStockTransferNoteCommand, CreateStockTransferNoteResultModel>
{
    public async Task<CreateStockTransferNoteResultModel> Handle(
        CreateStockTransferNoteCommand request, CancellationToken cancellationToken)
    {
        var dto = new CreateStockTransferNoteAppDto
        {
            FromWarehouseId = request.FromWarehouseId,
            ToWarehouseId = request.ToWarehouseId,
            Note = request.Note,
            Items = request.Items.Select(i => new CreateStockTransferNoteItemAppDto
            {
                ProductId = i.ProductId,
                ProductName = i.ProductName,
                Quantity = i.Quantity
            }).ToList()
        };
        var result = await appService.CreateAsync(dto).ConfigureAwait(false);

        return new CreateStockTransferNoteResultModel
        {
            Success = result.Success,
            ErrorMessage = result.ErrorMessage,
            CreatedId = result.CreatedId
        };
    }
}

public sealed class ApproveStockTransferNoteHandler(IStockTransferNoteAppService appService)
    : IRequestHandler<ApproveStockTransferNoteCommand, StockTransferNoteActionResultModel>
{
    public async Task<StockTransferNoteActionResultModel> Handle(
        ApproveStockTransferNoteCommand request, CancellationToken cancellationToken)
    {
        var result = await appService.ApproveAsync(request.Id).ConfigureAwait(false);
        return new StockTransferNoteActionResultModel { Success = result.Success, ErrorMessage = result.ErrorMessage };
    }
}

public sealed class CancelStockTransferNoteHandler(IStockTransferNoteAppService appService)
    : IRequestHandler<CancelStockTransferNoteCommand, StockTransferNoteActionResultModel>
{
    public async Task<StockTransferNoteActionResultModel> Handle(
        CancelStockTransferNoteCommand request, CancellationToken cancellationToken)
    {
        var result = await appService.CancelAsync(request.Id).ConfigureAwait(false);
        return new StockTransferNoteActionResultModel { Success = result.Success, ErrorMessage = result.ErrorMessage };
    }
}
