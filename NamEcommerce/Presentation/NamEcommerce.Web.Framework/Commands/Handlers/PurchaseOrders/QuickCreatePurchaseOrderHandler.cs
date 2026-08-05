using MediatR;
using NamEcommerce.Application.Contracts.Dtos.PurchaseOrders;
using NamEcommerce.Application.Contracts.PurchaseOrders;
using NamEcommerce.Web.Contracts.Commands.Models.PurchaseOrders;
using NamEcommerce.Web.Contracts.Models.PurchaseOrders;
using NamEcommerce.Web.Framework.Services;

namespace NamEcommerce.Web.Framework.Commands.Handlers.PurchaseOrders;

public sealed class QuickCreatePurchaseOrderHandler : IRequestHandler<PurchaseOrderQuickCreateCommand, QuickCreatePurchaseOrderResultModel>
{
    private readonly IPurchaseOrderAppService _purchaseOrderAppService;

    public QuickCreatePurchaseOrderHandler(IPurchaseOrderAppService appService)
    {
        _purchaseOrderAppService = appService;
    }

    public async Task<QuickCreatePurchaseOrderResultModel> Handle(PurchaseOrderQuickCreateCommand request, CancellationToken cancellationToken)
    {
        var result = await _purchaseOrderAppService.QuickCreatePurchaseOrderAsync(new PurchaseOrderQuickCreateAppDto
        {
            PlacedOnUtc = DateTimeHelper.ToUniversalTime(request.PlacedOn),
            VendorId = request.VendorId,
            DefaultWarehouseId = request.DefaultWarehouseId,
            ReceivedOnUtc = request.IsReceived ? DateTimeHelper.ToUniversalTime(request.ReceivedOn) : null,
            ExpectedDeliveryOnUtc = request.IsReceived || !request.ExpectedDeliveryDate.HasValue ? null : DateTimeHelper.ToUniversalTime(request.ExpectedDeliveryDate.Value),
            Note = request.Note,
            IsReceived = request.IsReceived,
            IsPaid = request.IsPaid,
            PictureIds = request.IsReceived ? request.PictureIds ?? [] : [],
            Items = request.Items.Select(i => new PurchaseOrderQuickCreateAppDto.PurchaseOrderQuickCreateItemAppDto
            {
                ProductId = i.ProductId,
                Quantity = i.Quantity,
                UnitCost = i.UnitCost,
                WarehouseId = i.WarehouseId
            }).ToList(),
            Payment = !request.IsPaid || request.PaymentInfo is null ? null : new PurchaseOrderQuickCreateAppDto.PurchaseOrderQuickCreatePaymentAppDto
            {
                PaidAmount = request.PaymentInfo.PaidAmount,
                PaymentMethod = request.PaymentInfo.PaymentMethod
            }
        }).ConfigureAwait(false);

        return new QuickCreatePurchaseOrderResultModel
        {
            Success = result.Success,
            ErrorMessage = result.ErrorMessage,
            CreatedId = result.CreatedId
        };
    }
}
