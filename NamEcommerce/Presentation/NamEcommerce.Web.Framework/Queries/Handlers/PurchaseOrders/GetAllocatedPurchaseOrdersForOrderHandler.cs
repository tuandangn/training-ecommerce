using MediatR;
using NamEcommerce.Application.Contracts.PurchaseOrders;
using NamEcommerce.Web.Contracts.Models.PurchaseOrders;
using NamEcommerce.Web.Contracts.Queries.Models.PurchaseOrders;

namespace NamEcommerce.Web.Framework.Queries.Handlers.PurchaseOrders;

public sealed class GetAllocatedPurchaseOrdersForOrderHandler
    : IRequestHandler<GetAllocatedPurchaseOrdersForOrderQuery, OrderAllocatedPurchaseOrderListModel>
{
    private const int DraftStatus = 10;
    private const int SubmittedStatus = 20;
    private const int ApprovedStatus = 30;
    private const int ReceivingStatus = 40;
    private const int CompletedStatus = 50;
    private const int CancelledStatus = 60;

    private readonly IPurchaseOrderAppService _purchaseOrderAppService;

    public GetAllocatedPurchaseOrdersForOrderHandler(IPurchaseOrderAppService purchaseOrderAppService)
    {
        _purchaseOrderAppService = purchaseOrderAppService;
    }

    public async Task<OrderAllocatedPurchaseOrderListModel> Handle(
        GetAllocatedPurchaseOrdersForOrderQuery request,
        CancellationToken cancellationToken)
    {
        var purchaseOrders = await _purchaseOrderAppService
            .GetAllocatedPurchaseOrdersForOrderAsync(request.OrderId)
            .ConfigureAwait(false);

        var items = purchaseOrders
            .OrderByDescending(purchaseOrder => purchaseOrder.PlacedOnUtc)
            .Select(purchaseOrder => new OrderAllocatedPurchaseOrderModel
            {
                PurchaseOrderId = purchaseOrder.PurchaseOrderId,
                PurchaseOrderCode = purchaseOrder.PurchaseOrderCode,
                Status = purchaseOrder.Status,
                StatusName = GetStatusName(purchaseOrder.Status),
                StatusClass = GetStatusClass(purchaseOrder.Status),
                VendorId = purchaseOrder.VendorId,
                VendorName = purchaseOrder.VendorName,
                PlacedOn = purchaseOrder.PlacedOnUtc.ToLocalTime(),
                ExpectedDeliveryDate = purchaseOrder.ExpectedDeliveryDateUtc?.ToLocalTime(),
                Items = purchaseOrder.Items.Select(item => new OrderAllocatedPurchaseOrderItemModel
                {
                    OrderItemId = item.OrderItemId,
                    ProductId = item.ProductId,
                    ProductName = item.ProductName,
                    AllocatedQuantity = item.AllocatedQuantity,
                    ReceivedQuantity = item.ReceivedQuantity
                }).ToList()
            })
            .ToList();

        return new OrderAllocatedPurchaseOrderListModel
        {
            OrderId = request.OrderId,
            Items = items
        };
    }

    private static string GetStatusName(int status)
        => status switch
        {
            DraftStatus => "Bản nháp",
            SubmittedStatus => "Đã gửi",
            ApprovedStatus => "Đã duyệt",
            ReceivingStatus => "Đang nhận",
            CompletedStatus => "Hoàn tất",
            CancelledStatus => "Đã hủy",
            _ => status.ToString()
        };

    private static string GetStatusClass(int status)
        => status switch
        {
            DraftStatus => "secondary",
            SubmittedStatus => "primary",
            ApprovedStatus => "info",
            ReceivingStatus => "warning",
            CompletedStatus => "success",
            CancelledStatus => "danger",
            _ => "secondary"
        };
}
