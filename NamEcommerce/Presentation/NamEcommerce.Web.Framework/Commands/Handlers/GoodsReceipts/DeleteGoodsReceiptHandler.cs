using MediatR;
using NamEcommerce.Application.Contracts.GoodsReceipts;
using NamEcommerce.Web.Contracts.Commands.Models.GoodsReceipts;
using NamEcommerce.Web.Contracts.Models.Common;

namespace NamEcommerce.Web.Framework.Commands.Handlers.GoodsReceipts;

public sealed class DeleteGoodsReceiptHandler : IRequestHandler<DeleteGoodsReceiptCommand, CommonActionResultModel>
{
    private readonly IGoodsReceiptAppService _goodsReceiptAppService;

    public DeleteGoodsReceiptHandler(IGoodsReceiptAppService goodsReceiptAppService)
    {
        _goodsReceiptAppService = goodsReceiptAppService;
    }

    public async Task<CommonActionResultModel> Handle(DeleteGoodsReceiptCommand request, CancellationToken cancellationToken)
    {
        var (success, errorMessage) = await _goodsReceiptAppService.DeleteGoodsReceiptAsync(request.Id).ConfigureAwait(false);

        return new CommonActionResultModel
        {
            Success = success,
            ErrorMessage = errorMessage
        };
    }
}
