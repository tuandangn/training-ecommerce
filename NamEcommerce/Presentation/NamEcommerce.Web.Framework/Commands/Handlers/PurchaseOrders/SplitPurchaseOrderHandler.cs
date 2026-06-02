using MediatR;
using NamEcommerce.Application.Contracts.Dtos.PurchaseOrders;
using NamEcommerce.Application.Contracts.PurchaseOrders;
using NamEcommerce.Web.Contracts.Commands.Models.PurchaseOrders;
using NamEcommerce.Web.Contracts.Models.PurchaseOrders;

namespace NamEcommerce.Web.Framework.Commands.Handlers.PurchaseOrders;

public sealed class SplitPurchaseOrderHandler(IPurchaseOrderAppService purchaseOrderAppService)
    : IRequestHandler<SplitPurchaseOrderCommand, CreatePurchaseOrderResultModel>
{
    public async Task<CreatePurchaseOrderResultModel> Handle(SplitPurchaseOrderCommand request, CancellationToken cancellationToken)
    {
        var result = await purchaseOrderAppService.SplitPurchaseOrderAsync(new SplitPurchaseOrderAppDto
        {
            PurchaseOrderId = request.PurchaseOrderId,
            Items = request.Items.Select(i => new SplitPurchaseOrderAppDto.SplitItemAppDto
            {
                ItemId = i.ItemId,
                Quantity = i.Quantity
            }).ToList()
        }).ConfigureAwait(false);

        return new CreatePurchaseOrderResultModel
        {
            Success = result.Success,
            ErrorMessage = result.ErrorMessage,
            CreatedId = result.CreatedId
        };
    }
}
