using MediatR;
using NamEcommerce.Application.Contracts.Dashboard;
using NamEcommerce.Web.Contracts.Models.Dashboard;
using NamEcommerce.Web.Contracts.Queries.Models.Dashboard;
using NamEcommerce.Web.Framework.Services;

namespace NamEcommerce.Web.Framework.Queries.Handlers.Dashboard;

public sealed class GetDashboardHandler : IRequestHandler<GetDashboardQuery, DashboardModel>
{
    private readonly IDashboardAppService _dashboardAppService;

    public GetDashboardHandler(IDashboardAppService dashboardAppService)
    {
        _dashboardAppService = dashboardAppService;
    }

    public async Task<DashboardModel> Handle(GetDashboardQuery request, CancellationToken cancellationToken)
    {
        var dto = await _dashboardAppService.GetDashboardDataAsync().ConfigureAwait(false);

        return new DashboardModel
        {
            SalesSummary = new SalesSummaryModel
            {
                TodayRevenue = dto.SalesSummary.TodayRevenue,
                MonthRevenue = dto.SalesSummary.MonthRevenue,
                QuarterRevenue = dto.SalesSummary.QuarterRevenue,
                YearRevenue = dto.SalesSummary.YearRevenue,
                RevenueTrend = dto.SalesSummary.RevenueTrendUtc.Select(point => new RevenueTrendPointModel
                {
                    Date = DateTimeHelper.ToLocalTime(point.DateUtc),
                    Revenue = point.Revenue,
                    Profit = point.Profit
                }).ToList()
            },
            ProfitSummary = new ProfitSummaryModel
            {
                TodayProfit = dto.ProfitSummary.TodayProfit,
                MonthProfit = dto.ProfitSummary.MonthProfit,
                QuarterProfit = dto.ProfitSummary.QuarterProfit,
                YearProfit = dto.ProfitSummary.YearProfit,
                MonthRevenue = dto.ProfitSummary.MonthRevenue,
                MonthCogs = dto.ProfitSummary.MonthCogs,
                MonthGrossProfit = dto.ProfitSummary.MonthGrossProfit,
                MonthOperatingExpenses = dto.ProfitSummary.MonthOperatingExpenses
            },
            PendingOrders = dto.PendingOrders.Select(order => new PendingOrderModel
            {
                Id = order.Id,
                Code = order.Code,
                CustomerName = order.CustomerName,
                TotalAmount = order.TotalAmount,
                PendingItemCount = order.PendingItemCount,
                ExpectedShippingDate = DateTimeHelper.ToLocalTime(order.ExpectedShippingDateUtc),
                CreatedOn = DateTimeHelper.ToLocalTime(order.CreatedOnUtc)
            }).ToList(),
            PendingPurchaseOrders = dto.PendingPurchaseOrders.Select(order => new PendingPurchaseOrderModel
            {
                Id = order.Id,
                Code = order.Code,
                VendorName = order.VendorName,
                TotalAmount = order.TotalAmount,
                RemainingQuantity = order.RemainingQuantity,
                ExpectedDeliveryDate = DateTimeHelper.ToLocalTime(order.ExpectedDeliveryDateUtc),
                CreatedOn = DateTimeHelper.ToLocalTime(order.CreatedOnUtc)
            }).ToList(),
            TopCustomerDebts = dto.TopCustomerDebts.Select(debt => new TopCustomerDebtModel
            {
                CustomerId = debt.CustomerId,
                CustomerName = debt.CustomerName,
                TotalRemainingAmount = debt.TotalRemainingAmount
            }).ToList(),
            TopVendorDebts = dto.TopVendorDebts.Select(debt => new TopVendorDebtModel
            {
                VendorId = debt.VendorId,
                VendorName = debt.VendorName,
                TotalRemainingAmount = debt.TotalRemainingAmount
            }).ToList(),
            LowStockProducts = dto.LowStockProducts.Select(product => new LowStockProductModel
            {
                InventoryStockId = product.InventoryStockId,
                ProductId = product.ProductId,
                ProductName = product.ProductName,
                WarehouseId = product.WarehouseId,
                WarehouseName = product.WarehouseName,
                QuantityOnHand = product.QuantityOnHand,
                QuantityReserved = product.QuantityReserved,
                QuantityAvailable = product.QuantityAvailable,
                ReorderLevel = product.ReorderLevel
            }).ToList()
        };
    }
}
