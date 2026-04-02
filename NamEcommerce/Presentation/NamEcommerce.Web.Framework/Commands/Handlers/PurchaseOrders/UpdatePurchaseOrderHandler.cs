using MediatR;
using NamEcommerce.Application.Contracts.Dtos.PurchaseOrders;
using NamEcommerce.Application.Contracts.PurchaseOrders;
using NamEcommerce.Web.Contracts.Commands.Models.PurchaseOrders;
using NamEcommerce.Web.Contracts.Models.Catalog;
using NamEcommerce.Web.Framework.Services;

namespace NamEcommerce.Web.Framework.Commands.Handlers.PurchaseOrders;

public sealed class UpdatePurchaseOrderHandler : IRequestHandler<UpdatePurchaseOrderCommand, UpdatePurchaseOrderResultModel>
{
    private readonly IPurchaseOrderAppService _purchaseOrderAppService;

    public UpdatePurchaseOrderHandler(IPurchaseOrderAppService purchaseOrderAppService)
    {
        _purchaseOrderAppService = purchaseOrderAppService;
    }

    public async Task<UpdatePurchaseOrderResultModel> Handle(UpdatePurchaseOrderCommand request, CancellationToken cancellationToken)
    {
        var updatePurchaseOrderResult = await _purchaseOrderAppService.UpdatePurchaseOrderAsync(new UpdatePurchaseOrderAppDto(request.Id)
        {
            VendorId = request.VendorId,
            ExpectedDeliveryDateUtc = DateTimeHelper.ToUniversalTime(request.ExpectedDeliveryDate),
            Note = request.Note,
            TaxAmount = request.TaxAmount,
            ShippingAmount = request.ShippingAmount
        });

        return new UpdatePurchaseOrderResultModel
        {
            Success = updatePurchaseOrderResult.Success,
            ErrorMessage = updatePurchaseOrderResult.ErrorMessage
        };
    }
}
