using MediatR;
using NamEcommerce.Application.Contracts.Dtos.GoodsReceipts;
using NamEcommerce.Application.Contracts.GoodsReceipts;
using NamEcommerce.Web.Contracts.Commands.Models.GoodsReceipts;
using NamEcommerce.Web.Contracts.Models.GoodsReceipts;

namespace NamEcommerce.Web.Framework.Commands.Handlers.GoodsReceipts;

public sealed class SetGoodsReceiptItemUnitCostHandler : IRequestHandler<SetGoodsReceiptItemUnitCostCommand, SetGoodsReceiptItemUnitCostResultModel>
{
    private readonly IGoodsReceiptAppService _goodsReceiptAppService;

    public SetGoodsReceiptItemUnitCostHandler(IGoodsReceiptAppService goodsReceiptAppService)
    {
        _goodsReceiptAppService = goodsReceiptAppService;
    }

    public async Task<SetGoodsReceiptItemUnitCostResultModel> Handle(SetGoodsReceiptItemUnitCostCommand request, CancellationToken cancellationToken)
    {
        var result = await _goodsReceiptAppService.SetGoodsReceiptItemUnitCostAsync(new SetGoodsReceiptItemUnitCostAppDto
        {
            GoodsReceiptId = request.GoodsReceiptId,
            GoodsReceiptItemId = request.GoodsReceiptItemId,
            UnitCost = request.UnitCost
        }).ConfigureAwait(false);

        return new SetGoodsReceiptItemUnitCostResultModel
        {
            Success = result.Success,
            ErrorMessage = result.ErrorMessage
        };
    }
}
