using System.Globalization;
using NamEcommerce.Application.Contracts.Dashboard;
using NamEcommerce.Application.Contracts.Dtos.Dashboard;
using NamEcommerce.Application.Contracts.Dtos.Report;
using NamEcommerce.Application.Contracts.Report;
using NamEcommerce.Application.Services.Extensions;
using NamEcommerce.Domain.Entities.Catalog;
using NamEcommerce.Domain.Entities.Customers;
using NamEcommerce.Domain.Entities.Debts;
using NamEcommerce.Domain.Entities.Inventory;
using NamEcommerce.Domain.Entities.Orders;
using NamEcommerce.Domain.Entities.PurchaseOrders;
using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Domain.Shared.Enums.Orders;
using NamEcommerce.Domain.Shared.Enums.PurchaseOrders;

namespace NamEcommerce.Application.Services.Dashboard;

public sealed class DashboardAppService(
    IFinancialReportAppService financialReportAppService,
    IEntityDataReader<Order> orderReader,
    IEntityDataReader<PurchaseOrder> purchaseOrderReader,
    IEntityDataReader<CustomerDebt> customerDebtReader,
    IEntityDataReader<VendorDebt> vendorDebtReader,
    IEntityDataReader<InventoryStock> inventoryStockReader,
    IEntityDataReader<Customer> customerReader,
    IEntityDataReader<Vendor> vendorReader,
    IEntityDataReader<Product> productReader,
    IEntityDataReader<Warehouse> warehouseReader) : IDashboardAppService
{
    public async Task<DashboardAppDto> GetDashboardDataAsync()
    {
        var today = DateTime.Today;
        var monthStart = new DateTime(today.Year, today.Month, 1);
        var quarterStart = new DateTime(today.Year, ((today.Month - 1) / 3) * 3 + 1, 1);
        var yearStart = new DateTime(today.Year, 1, 1);
        var trendStart = today.AddDays(-29);

        var todaySummary = await financialReportAppService.GetProfitLossSummaryAsync(today, today).ConfigureAwait(false);
        var monthSummary = await financialReportAppService.GetProfitLossSummaryAsync(monthStart, today).ConfigureAwait(false);
        var quarterSummary = await financialReportAppService.GetProfitLossSummaryAsync(quarterStart, today).ConfigureAwait(false);
        var yearSummary = await financialReportAppService.GetProfitLossSummaryAsync(yearStart, today).ConfigureAwait(false);
        var trendSummary = await financialReportAppService.GetProfitLossSummaryAsync(trendStart, today).ConfigureAwait(false);

        return new DashboardAppDto
        {
            SalesSummary = BuildSalesSummary(todaySummary, monthSummary, quarterSummary, yearSummary, trendSummary, trendStart, today),
            ProfitSummary = BuildProfitSummary(todaySummary, monthSummary, quarterSummary, yearSummary),
            PendingOrders = GetPendingOrders(),
            PendingPurchaseOrders = GetPendingPurchaseOrders(),
            TopCustomerDebts = GetTopCustomerDebts(),
            TopVendorDebts = GetTopVendorDebts(),
            LowStockProducts = GetLowStockProducts()
        };
    }

    private static SalesSummaryAppDto BuildSalesSummary(
        ProfitLossSummaryAppDto todaySummary,
        ProfitLossSummaryAppDto monthSummary,
        ProfitLossSummaryAppDto quarterSummary,
        ProfitLossSummaryAppDto yearSummary,
        ProfitLossSummaryAppDto trendSummary,
        DateTime trendStart,
        DateTime today)
        => new()
        {
            TodayRevenue = todaySummary.TotalRevenue,
            MonthRevenue = monthSummary.TotalRevenue,
            QuarterRevenue = quarterSummary.TotalRevenue,
            YearRevenue = yearSummary.TotalRevenue,
            RevenueTrendUtc = BuildRevenueTrend(trendSummary, trendStart, today)
        };

    private static ProfitSummaryAppDto BuildProfitSummary(
        ProfitLossSummaryAppDto todaySummary,
        ProfitLossSummaryAppDto monthSummary,
        ProfitLossSummaryAppDto quarterSummary,
        ProfitLossSummaryAppDto yearSummary)
        => new()
        {
            TodayProfit = todaySummary.NetProfit,
            MonthProfit = monthSummary.NetProfit,
            QuarterProfit = quarterSummary.NetProfit,
            YearProfit = yearSummary.NetProfit,
            MonthRevenue = monthSummary.TotalRevenue,
            MonthCogs = monthSummary.TotalCogs,
            MonthGrossProfit = monthSummary.GrossProfit,
            MonthOperatingExpenses = monthSummary.TotalOperatingExpenses
        };

    private static IReadOnlyCollection<RevenueTrendPointAppDto> BuildRevenueTrend(
        ProfitLossSummaryAppDto trendSummary,
        DateTime trendStart,
        DateTime today)
    {
        var trendByDate = trendSummary.RevenueTrend
            .Select(point => new
            {
                Date = DateTime.ParseExact(point.DateLabel, "dd/MM/yyyy", CultureInfo.InvariantCulture).Date,
                point.Revenue,
                point.Profit
            })
            .ToDictionary(point => point.Date, point => point);

        var points = new List<RevenueTrendPointAppDto>();
        for (var date = trendStart.Date; date <= today.Date; date = date.AddDays(1))
        {
            trendByDate.TryGetValue(date, out var point);
            points.Add(new RevenueTrendPointAppDto
            {
                DateUtc = date.ToUniversalTime(),
                Revenue = point?.Revenue ?? 0m,
                Profit = point?.Profit ?? 0m
            });
        }

        return points;
    }

    private IReadOnlyCollection<PendingOrderAppDto> GetPendingOrders()
    {
        var customerNames = customerReader.DataSource.ToDictionary(customer => customer.Id, customer => customer.FullName);

        return orderReader.DataSource
            .Where(order => order.OrderStatus != OrderStatus.Cancelled && order.OrderItems.Any(item => !item.IsDelivered))
            .OrderBy(order => order.ExpectedShippingDateUtc ?? order.CreatedOnUtc)
            .ThenBy(order => order.CreatedOnUtc)
            .Take(5)
            .ToList()
            .Select(order => order.ToPendingOrderAppDto(customerNames.GetValueOrDefault(order.CustomerId, "(Không xác định)")))
            .ToList();
    }

    private IReadOnlyCollection<PendingPurchaseOrderAppDto> GetPendingPurchaseOrders()
    {
        var vendorNames = vendorReader.DataSource.ToDictionary(vendor => vendor.Id, vendor => vendor.Name);

        return purchaseOrderReader.DataSource
            .Where(order => order.Status != PurchaseOrderStatus.Cancelled
                && order.Status != PurchaseOrderStatus.Completed
                && order.Items.Any(item => item.QuantityReceived < item.QuantityOrdered))
            .OrderBy(order => order.ExpectedDeliveryDateUtc ?? order.CreatedOnUtc)
            .ThenBy(order => order.CreatedOnUtc)
            .Take(5)
            .ToList()
            .Select(order => order.ToPendingPurchaseOrderAppDto(vendorNames.GetValueOrDefault(order.VendorId, "(Không xác định)")))
            .ToList();
    }

    private IReadOnlyCollection<TopCustomerDebtAppDto> GetTopCustomerDebts()
    {
        var customerNames = customerReader.DataSource.ToDictionary(customer => customer.Id, customer => customer.FullName);

        return customerDebtReader.DataSource
            .Where(debt => debt.RemainingAmount > 0)
            .ToList()
            .GroupBy(debt => debt.CustomerId)
            .Select(group => group.ToTopCustomerDebtAppDto(
                group.Key,
                customerNames.GetValueOrDefault(group.Key, group.First().CustomerName)))
            .OrderByDescending(item => item.TotalRemainingAmount)
            .Take(5)
            .ToList();
    }

    private IReadOnlyCollection<TopVendorDebtAppDto> GetTopVendorDebts()
    {
        var vendorNames = vendorReader.DataSource.ToDictionary(vendor => vendor.Id, vendor => vendor.Name);

        return vendorDebtReader.DataSource
            .Where(debt => debt.RemainingAmount > 0)
            .ToList()
            .GroupBy(debt => debt.VendorId)
            .Select(group => group.ToTopVendorDebtAppDto(
                group.Key,
                vendorNames.GetValueOrDefault(group.Key, group.First().VendorName)))
            .OrderByDescending(item => item.TotalRemainingAmount)
            .Take(5)
            .ToList();
    }

    private IReadOnlyCollection<LowStockProductAppDto> GetLowStockProducts()
    {
        var productNames = productReader.DataSource.ToDictionary(product => product.Id, product => product.Name);
        var warehouseNames = warehouseReader.DataSource.ToDictionary(warehouse => warehouse.Id, warehouse => warehouse.Name);

        return inventoryStockReader.DataSource
            .Where(stock => stock.ReorderLevel > 0 && stock.QuantityOnHand <= stock.ReorderLevel)
            .ToList()
            .OrderBy(stock => stock.QuantityOnHand / stock.ReorderLevel)
            .ThenBy(stock => productNames.GetValueOrDefault(stock.ProductId, "(Không xác định)"))
            .Take(10)
            .Select(stock => stock.ToLowStockProductAppDto(
                productNames.GetValueOrDefault(stock.ProductId, "(Không xác định)"),
                warehouseNames.GetValueOrDefault(stock.WarehouseId, "(Không xác định)")))
            .ToList();
    }
}
