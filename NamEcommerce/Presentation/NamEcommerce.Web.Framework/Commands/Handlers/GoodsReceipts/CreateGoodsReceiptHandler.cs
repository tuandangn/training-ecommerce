using MediatR;
using NamEcommerce.Application.Contracts.Dtos.GoodsReceipts;
using NamEcommerce.Application.Contracts.GoodsReceipts;
using NamEcommerce.Web.Contracts.Commands.Models.GoodsReceipts;
using NamEcommerce.Web.Contracts.Models.GoodsReceipts;
using NamEcommerce.Web.Framework.Services;

namespace NamEcommerce.Web.Framework.Commands.Handlers.GoodsReceipts;

public sealed class CreateGoodsReceiptHandler : IRequestHandler<CreateGoodsReceiptCommand, CreateGoodsReceiptResultModel>
{
    private readonly IGoodsReceiptAppService _goodsReceiptAppService;

    public CreateGoodsReceiptHandler(IGoodsReceiptAppService goodsReceiptAppService)
    {
        _goodsReceiptAppService = goodsReceiptAppService;
    }

    public async Task<CreateGoodsReceiptResultModel> Handle(CreateGoodsReceiptCommand request, CancellationToken cancellationToken)
    {
        var result = await _goodsReceiptAppService.CreateGoodsReceiptAsync(new CreateGoodsReceiptAppDto
        {
            CreatedOnUtc = DateTimeHelper.ToUniversalTime(request.CreatedOn),
            TruckDriverName = request.TruckDriverName,
            TruckNumberSerial = request.TruckNumberSerial,
            PictureIds = request.PictureIds,
            Note = request.Note,
            VendorId = request.VendorId,
            Items = request.Items.Select(i => new CreateGoodsReceiptItemAppDto
            {
                ProductId = i.ProductId,
                WarehouseId = i.WarehouseId,
                Quantity = i.Quantity,
                UnitCost = i.UnitCost
            }).ToList()
        }).ConfigureAwait(false);

        return new CreateGoodsReceiptResultModel
        {
            Success = result.Success,
            ErrorMessage = result.ErrorMessage,
            CreatedId = result.CreatedId
        };
    }
}
