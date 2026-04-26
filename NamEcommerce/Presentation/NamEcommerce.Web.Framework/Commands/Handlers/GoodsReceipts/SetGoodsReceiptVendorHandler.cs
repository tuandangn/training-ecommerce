using MediatR;
using NamEcommerce.Application.Contracts.Dtos.GoodsReceipts;
using NamEcommerce.Application.Contracts.GoodsReceipts;
using NamEcommerce.Web.Contracts.Commands.Models.GoodsReceipts;
using NamEcommerce.Web.Contracts.Models.GoodsReceipts;

namespace NamEcommerce.Web.Framework.Commands.Handlers.GoodsReceipts;

public sealed class SetGoodsReceiptVendorHandler : IRequestHandler<SetGoodsReceiptVendorCommand, SetGoodsReceiptVendorResultModel>
{
    private readonly IGoodsReceiptAppService _goodsReceiptAppService;

    public SetGoodsReceiptVendorHandler(IGoodsReceiptAppService goodsReceiptAppService)
    {
        _goodsReceiptAppService = goodsReceiptAppService;
    }

    public async Task<SetGoodsReceiptVendorResultModel> Handle(SetGoodsReceiptVendorCommand request, CancellationToken cancellationToken)
    {
        var result = await _goodsReceiptAppService.SetGoodsReceiptVendorAsync(new SetGoodsReceiptVendorAppDto(request.GoodsReceiptId)
        {
            VendorId = request.VendorId
        }).ConfigureAwait(false);

        return new SetGoodsReceiptVendorResultModel
        {
            Success = result.Success,
            UpdatedId = result.UpdatedId,
            ErrorMessage = result.ErrorMessage
        };
    }
}
