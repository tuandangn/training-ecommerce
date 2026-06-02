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
                TodayGrossRevenue = dto.SalesSummary.TodayGrossRevenue,
                TodayReturnAmount = dto.SalesSummary.TodayReturnAmount,
                MonthRevenue = dto.SalesSummary.MonthRevenue,
                MonthGrossRevenue = dto.SalesSummary.MonthGrossRevenue,
                MonthReturnAmount = dto.SalesSummary.MonthReturnAmount,
                QuarterRevenue = dto.SalesSummary.QuarterRevenue,
                QuarterGrossRevenue = dto.SalesSummary.QuarterGrossRevenue,
                QuarterReturnAmount = dto.SalesSummary.QuarterReturnAmount,
                YearRevenue = dto.SalesSummary.YearRevenue,
                YearGrossRevenue = dto.SalesSummary.YearGrossRevenue,
                YearReturnAmount = dto.SalesSummary.YearReturnAmount,
                RevenueTrend = dto.SalesSummary.RevenueTrendUtc.Select(point => new RevenueTrendPointModel
                {
                    Date = DateTimeHelper.ToLocalTime(point.DateUtc),
                    Revenue = point.Revenue,
                    Profit = point.Profit
                }).ToList()
            },
            ProfitSummary = new ProfitSummaryModel
            {
                TodayRevenue = dto.ProfitSummary.TodayRevenue,
                TodayCogs = dto.ProfitSummary.TodayCogs,
                TodayGrossProfit = dto.ProfitSummary.TodayGrossProfit,
                TodayOperatingExpenses = dto.ProfitSummary.TodayOperatingExpenses,
                TodayProfit = dto.ProfitSummary.TodayProfit,

                MonthRevenue = dto.ProfitSummary.MonthRevenue,
                MonthCogs = dto.ProfitSummary.MonthCogs,
                MonthGrossProfit = dto.ProfitSummary.MonthGrossProfit,
                MonthOperatingExpenses = dto.ProfitSummary.MonthOperatingExpenses,
                MonthProfit = dto.ProfitSummary.MonthProfit,

                QuarterRevenue = dto.ProfitSummary.QuarterRevenue,
                QuarterCogs = dto.ProfitSummary.QuarterCogs,
                QuarterGrossProfit = dto.ProfitSummary.QuarterGrossProfit,
                QuarterOperatingExpenses = dto.ProfitSummary.QuarterOperatingExpenses,
                QuarterProfit = dto.ProfitSummary.QuarterProfit,

                YearRevenue = dto.ProfitSummary.YearRevenue,
                YearCogs = dto.ProfitSummary.YearCogs,
                YearGrossProfit = dto.ProfitSummary.YearGrossProfit,
                YearOperatingExpenses = dto.ProfitSummary.YearOperatingExpenses,
                YearProfit = dto.ProfitSummary.YearProfit
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
