using MediatR;
using NamEcommerce.Application.Contracts.Dtos.GoodsReceipts;
using NamEcommerce.Application.Contracts.GoodsReceipts;
using NamEcommerce.Web.Contracts.Commands.Models.GoodsReceipts;
using NamEcommerce.Web.Contracts.Models.GoodsReceipts;
using NamEcommerce.Web.Framework.Services;

namespace NamEcommerce.Web.Framework.Commands.Handlers.GoodsReceipts;

public sealed class UpdateGoodsReceiptHandler : IRequestHandler<UpdateGoodsReceiptCommand, UpdateGoodsReceiptResultModel>
{
    private readonly IGoodsReceiptAppService _goodsReceiptAppService;

    public UpdateGoodsReceiptHandler(IGoodsReceiptAppService goodsReceiptAppService)
    {
        _goodsReceiptAppService = goodsReceiptAppService;
    }

    public async Task<UpdateGoodsReceiptResultModel> Handle(UpdateGoodsReceiptCommand request, CancellationToken cancellationToken)
    {
        var result = await _goodsReceiptAppService.UpdateGoodsReceiptAsync(new UpdateGoodsReceiptAppDto(request.Id)
        {
            CreatedOnUtc = DateTimeHelper.ToUniversalTime(request.CreatedOn),
            TruckDriverName = request.TruckDriverName,
            TruckNumberSerial = request.TruckNumberSerial,
            PictureIds = request.PictureIds,
            Note = request.Note
        }).ConfigureAwait(false);

        return new UpdateGoodsReceiptResultModel
        {
            Success = result.Success,
            ErrorMessage = result.ErrorMessage,
            UpdatedId = result.UpdatedId
        };
    }
}
