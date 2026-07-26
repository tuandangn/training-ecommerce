using MediatR;
using NamEcommerce.Application.Contracts.Inventory;
using NamEcommerce.Web.Contracts.Models.Inventory;
using NamEcommerce.Web.Contracts.Queries.Models.Inventory;
using NamEcommerce.Web.Framework.Common;
using NamEcommerce.Web.Framework.Services;

namespace NamEcommerce.Web.Framework.Queries.Handlers.Inventory;

public sealed class GetProductReservationLedgerHandler : IRequestHandler<GetProductReservationLedgerQuery, ProductReservationLedgerListModel>
{
    private readonly IInventoryAppService _inventoryAppService;

    public GetProductReservationLedgerHandler(IInventoryAppService inventoryAppService)
    {
        _inventoryAppService = inventoryAppService;
    }

    public async Task<ProductReservationLedgerListModel> Handle(GetProductReservationLedgerQuery request, CancellationToken cancellationToken)
    {
        var pagedData = await _inventoryAppService.GetProductReservationLedgerAsync(request.ProductId, request.PageIndex, request.PageSize);

        return new ProductReservationLedgerListModel
        {
            ProductId = request.ProductId,
            ProductName = pagedData.Items.FirstOrDefault()?.ProductName ?? string.Empty,
            Data = pagedData.MapToModel(item => new ProductReservationLedgerListModel.ItemModel(item.Id)
            {
                ProductId = item.ProductId,
                OrderId = item.OrderId,
                OrderCode = item.OrderCode,
                QuantityDelta = item.QuantityDelta,
                UnitPrice = item.UnitPrice,
                Reason = item.Reason,
                ReferenceId = item.ReferenceId,
                CreatedOn = DateTimeHelper.ToLocalTime(item.CreatedOnUtc)
            })
        };
    }
}
