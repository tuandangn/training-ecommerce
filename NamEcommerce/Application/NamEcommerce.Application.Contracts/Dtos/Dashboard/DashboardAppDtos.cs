namespace NamEcommerce.Application.Contracts.Dtos.Dashboard;

[Serializable]
public sealed record DashboardAppDto
{
    public required SalesSummaryAppDto SalesSummary { get; init; }
    public required ProfitSummaryAppDto ProfitSummary { get; init; }
    public IReadOnlyCollection<PendingOrderAppDto> PendingOrders { get; init; } = [];
    public IReadOnlyCollection<PendingPurchaseOrderAppDto> PendingPurchaseOrders { get; init; } = [];
    public IReadOnlyCollection<TopCustomerDebtAppDto> TopCustomerDebts { get; init; } = [];
    public IReadOnlyCollection<TopVendorDebtAppDto> TopVendorDebts { get; init; } = [];
    public IReadOnlyCollection<LowStockProductAppDto> LowStockProducts { get; init; } = [];
}

[Serializable]
public sealed record SalesSummaryAppDto
{
    public required decimal TodayRevenue { get; init; }
    public required decimal MonthRevenue { get; init; }
    public required decimal QuarterRevenue { get; init; }
    public required decimal YearRevenue { get; init; }
    public IReadOnlyCollection<RevenueTrendPointAppDto> RevenueTrendUtc { get; init; } = [];
}

[Serializable]
public sealed record RevenueTrendPointAppDto
{
    public required DateTime DateUtc { get; init; }
    public required decimal Revenue { get; init; }
    public required decimal Profit { get; init; }
}

[Serializable]
public sealed record ProfitSummaryAppDto
{
    public required decimal TodayProfit { get; init; }
    public required decimal MonthProfit { get; init; }
    public required decimal QuarterProfit { get; init; }
    public required decimal YearProfit { get; init; }
    public required decimal MonthRevenue { get; init; }
    public required decimal MonthCogs { get; init; }
    public required decimal MonthGrossProfit { get; init; }
    public required decimal MonthOperatingExpenses { get; init; }
}

[Serializable]
public sealed record PendingOrderAppDto
{
    public required Guid Id { get; init; }
    public required string Code { get; init; }
    public required string CustomerName { get; init; }
    public required decimal TotalAmount { get; init; }
    public required int PendingItemCount { get; init; }
    public DateTime? ExpectedShippingDateUtc { get; init; }
    public required DateTime CreatedOnUtc { get; init; }
}

[Serializable]
public sealed record PendingPurchaseOrderAppDto
{
    public required Guid Id { get; init; }
    public required string Code { get; init; }
    public required string VendorName { get; init; }
    public required decimal TotalAmount { get; init; }
    public required decimal RemainingQuantity { get; init; }
    public DateTime? ExpectedDeliveryDateUtc { get; init; }
    public required DateTime CreatedOnUtc { get; init; }
}

[Serializable]
public sealed record TopCustomerDebtAppDto
{
    public required Guid CustomerId { get; init; }
    public required string CustomerName { get; init; }
    public required decimal TotalRemainingAmount { get; init; }
}

[Serializable]
public sealed record TopVendorDebtAppDto
{
    public required Guid VendorId { get; init; }
    public required string VendorName { get; init; }
    public required decimal TotalRemainingAmount { get; init; }
}

[Serializable]
public sealed record LowStockProductAppDto
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
