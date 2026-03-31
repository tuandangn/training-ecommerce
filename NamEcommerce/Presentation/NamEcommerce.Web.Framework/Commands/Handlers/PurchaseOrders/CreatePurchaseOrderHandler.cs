using MediatR;
using NamEcommerce.Application.Contracts.PurchaseOrders;
using NamEcommerce.Web.Contracts.Models.PurchaseOrders;
using NamEcommerce.Web.Contracts.Commands.Models.PurchaseOrders;
using NamEcommerce.Application.Contracts.Dtos.PurchaseOrders;

namespace NamEcommerce.Web.Framework.Commands.Handlers.PurchaseOrders;

public sealed class CreatePurchaseOrderHandler : IRequestHandler<CreatePurchaseOrderCommand, CreatePurchaseOrderResultModel>
{
    private readonly IPurchaseOrderAppService _purchaseOrderAppService;

    public CreatePurchaseOrderHandler(IPurchaseOrderAppService appService)
    {
        _purchaseOrderAppService = appService;
    }

    public async Task<CreatePurchaseOrderResultModel> Handle(CreatePurchaseOrderCommand request, CancellationToken cancellationToken)
    {
        var result = await _purchaseOrderAppService.CreatePurchaseOrderAsync(new CreatePurchaseOrderAppDto
        {
            VendorId = request.VendorId,
            WarehouseId = request.WarehouseId,
            Note = request.Note,
            ExpectedDeliveryDateUtc = request.ExpectedDeliveryDate?.ToUniversalTime(),
            CreatedByUserId = request.CreatedByUserId,
            ShippingAmount = request.ShippingAmount,
            TaxAmount = request.TaxAmount
        }).ConfigureAwait(false);

        return new CreatePurchaseOrderResultModel
        {
            Success = result.Success,
            ErrorMessage = result.ErrorMessage,
            CreatedId = result.CreatedId
        };
    }
}
