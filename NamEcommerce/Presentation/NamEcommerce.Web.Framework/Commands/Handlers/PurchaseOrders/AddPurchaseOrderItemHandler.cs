using MediatR;
using NamEcommerce.Application.Contracts.PurchaseOrders;
using NamEcommerce.Web.Contracts.Models.PurchaseOrders;
using NamEcommerce.Web.Contracts.Commands.Models.PurchaseOrders;
using NamEcommerce.Application.Contracts.Dtos.PurchaseOrders;

namespace NamEcommerce.Web.Framework.Commands.Handlers.PurchaseOrders;

public sealed class AddPurchaseOrderItemHandler : IRequestHandler<AddPurchaseOrderItemCommand, AddPurchaseOrderItemResultModel>
{
    private readonly IPurchaseOrderAppService _purchaseOrderAppService;

    public AddPurchaseOrderItemHandler(IPurchaseOrderAppService appService)
    {
        _purchaseOrderAppService = appService;
    }

    public async Task<AddPurchaseOrderItemResultModel> Handle(AddPurchaseOrderItemCommand request, CancellationToken cancellationToken)
    {
        var result = await _purchaseOrderAppService.AddPurchaseOrderItemAsync(new AddPurchaseOrderItemAppDto
        {
            PurchaseOrderId = request.PurchaseOrderId,
            ProductId = request.ProductId,
            QuantityOrdered = request.Quantity,
            UnitCost = request.UnitCost,
            Note = request.Note
        }).ConfigureAwait(false);

        return new AddPurchaseOrderItemResultModel
        {
            Success = result.Success,
            ErrorMessage = result.ErrorMessage
        };
    }
}
