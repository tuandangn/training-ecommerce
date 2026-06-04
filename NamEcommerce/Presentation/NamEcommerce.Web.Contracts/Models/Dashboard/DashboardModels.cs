namespace NamEcommerce.Web.Contracts.Models.Dashboard;

[Serializable]
public sealed record DashboardModel
{
    public required SalesSummaryModel SalesSummary { get; init; }
    public required ProfitSummaryModel ProfitSummary { get; init; }
    public IReadOnlyCollection<PendingOrderModel> PendingOrders { get; init; } = [];
    public IReadOnlyCollection<PendingPurchaseOrderModel> PendingPurchaseOrders { get; init; } = [];
    public IReadOnlyCollection<TopCustomerDebtModel> TopCustomerDebts { get; init; } = [];
    public IReadOnlyCollection<TopVendorDebtModel> TopVendorDebts { get; init; } = [];
    public IReadOnlyCollection<LowStockProductModel> LowStockProducts { get; init; } = [];
}

[Serializable]
public sealed record SalesSummaryModel
{
    public required decimal TodayRevenue { get; init; }
    public required decimal TodayGrossRevenue { get; init; }
    public required decimal TodayReturnAmount { get; init; }
    public required decimal MonthRevenue { get; init; }
    public required decimal MonthGrossRevenue { get; init; }
    public required decimal MonthReturnAmount { get; init; }
    public required decimal QuarterRevenue { get; init; }
    public required decimal QuarterGrossRevenue { get; init; }
    public required decimal QuarterReturnAmount { get; init; }
    public required decimal YearRevenue { get; init; }
    public required decimal YearGrossRevenue { get; init; }
    public required decimal YearReturnAmount { get; init; }
    public IReadOnlyCollection<RevenueTrendPointModel> RevenueTrend { get; init; } = [];
}

[Serializable]
public sealed record RevenueTrendPointModel
{
    public required DateTime Date { get; init; }
    public required decimal Revenue { get; init; }
    public required decimal Profit { get; init; }
}

[Serializable]
public sealed record ProfitSummaryModel
{
    public required decimal TodayRevenue { get; init; }
    public required decimal TodayCogs { get; init; }
    public required decimal TodayGrossProfit { get; init; }
    public required decimal TodayOperatingExpenses { get; init; }
    public required decimal TodayProfit { get; init; }

    public required decimal MonthRevenue { get; init; }
    public required decimal MonthCogs { get; init; }
    public required decimal MonthGrossProfit { get; init; }
    public required decimal MonthOperatingExpenses { get; init; }
    public required decimal MonthProfit { get; init; }

    public required decimal QuarterRevenue { get; init; }
    public required decimal QuarterCogs { get; init; }
    public required decimal QuarterGrossProfit { get; init; }
    public required decimal QuarterOperatingExpenses { get; init; }
    public required decimal QuarterProfit { get; init; }

    public required decimal YearRevenue { get; init; }
    public required decimal YearCogs { get; init; }
    public required decimal YearGrossProfit { get; init; }
    public required decimal YearOperatingExpenses { get; init; }
    public required decimal YearProfit { get; init; }
}

[Serializable]
public sealed record PendingOrderModel
{
    public required Guid Id { get; init; }
    public required string Code { get; init; }
    public required string CustomerName { get; init; }
    public required decimal TotalAmount { get; init; }
    public required int PendingItemCount { get; init; }
    public DateTime? ExpectedShippingDate { get; init; }
    public required DateTime CreatedOn { get; init; }
}

[Serializable]
public sealed record PendingPurchaseOrderModel
{
    public required Guid Id { get; init; }
    public required string Code { get; init; }
    public required string VendorName { get; init; }
    public required decimal TotalAmount { get; init; }
    public required decimal RemainingQuantity { get; init; }
    public DateTime? ExpectedDeliveryDate { get; init; }
    public required DateTime CreatedOn { get; init; }
}

[Serializable]
public sealed record TopCustomerDebtModel
{
    public required Guid CustomerId { get; init; }
    public required string CustomerName { get; init; }
    public required decimal TotalRemainingAmount { get; init; }
}

[Serializable]
public sealed record TopVendorDebtModel
{
    public required Guid VendorId { get; init; }
    public required string VendorName { get; init; }
    public required decimal TotalRemainingAmount { get; init; }
}

[Serializable]
public sealed record LowStockProductModel
{
    public required Guid InventoryStockId { get; init; }
    public required Guid ProductId { get; init; }
    public required string ProductName { get; init; }
    public required Guid WarehouseId { get; init; }
    public required string WarehouseName { get; init; }
    public required decimal QuantityOnHand { get; init; }
    public required decimal QuantityReserved { get; init; }
    public required decimal QuantityAvailable { get; init; }
    public required decimal ReorderLevel { get; init; }
}
