using MediatR;
using NamEcommerce.Application.Contracts.Dtos.PurchaseOrders;
using NamEcommerce.Application.Contracts.PurchaseOrders;
using NamEcommerce.Web.Contracts.Commands.Models.PurchaseOrders;
using NamEcommerce.Web.Contracts.Models.PurchaseOrders;
using NamEcommerce.Web.Framework.Services;

namespace NamEcommerce.Web.Framework.Commands.Handlers.PurchaseOrders;

public sealed class QuickCreatePurchaseOrderHandler : IRequestHandler<QuickCreatePurchaseOrderCommand, QuickCreatePurchaseOrderResultModel>
{
    private readonly IPurchaseOrderAppService _purchaseOrderAppService;

    public QuickCreatePurchaseOrderHandler(IPurchaseOrderAppService appService)
    {
        _purchaseOrderAppService = appService;
    }

    public async Task<QuickCreatePurchaseOrderResultModel> Handle(QuickCreatePurchaseOrderCommand request, CancellationToken cancellationToken)
    {
        var result = await _purchaseOrderAppService.QuickCreatePurchaseOrderAsync(new QuickCreatePurchaseOrderAppDto
        {
            VendorId = request.VendorId,
            DefaultWarehouseId = request.DefaultWarehouseId,
            ReceivedOnUtc = DateTimeHelper.ToUniversalTime(request.ReceivedOn),
            Note = request.Note,
            ReceiveImmediately = request.ReceiveImmediately,
            PictureIds = request.PictureIds,
            Items = request.Items.Select(i => new QuickCreatePurchaseOrderItemAppDto
            {
                ProductId = i.ProductId,
                Quantity = i.Quantity,
                UnitCost = i.UnitCost,
                WarehouseId = i.WarehouseId,
                QuantityDecimalPlaces = i.QuantityDecimalPlaces
            }).ToList(),
            Payment = request.Payment is null ? null : new QuickCreatePaymentAppDto
            {
                Amount = request.Payment.Amount,
                PaymentMethod = (int)request.Payment.PaymentMethod
            }
        }).ConfigureAwait(false);

        return new QuickCreatePurchaseOrderResultModel
        {
            Success = result.Success,
            ErrorMessage = result.ErrorMessage,
            PurchaseOrderId = result.PurchaseOrderId,
            PurchaseOrderCode = result.PurchaseOrderCode,
            GoodsReceiptIds = result.GoodsReceiptIds
        };
    }
}
