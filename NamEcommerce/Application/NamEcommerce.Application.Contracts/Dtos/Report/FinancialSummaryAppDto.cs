namespace NamEcommerce.Application.Contracts.Dtos.Report;

public class ProfitLossSummaryAppDto
{
    public decimal TotalRevenue { get; set; }
    public decimal TotalCogs { get; set; } // Cost of Goods Sold
    public decimal GrossProfit => TotalRevenue - TotalCogs;
    
    public decimal TotalOperatingExpenses { get; set; }
    public decimal NetProfit => GrossProfit - TotalOperatingExpenses;
    
    public List<RevenueByDateAppDto> RevenueTrend { get; set; } = new();
    public List<TopSellingProductAppDto> TopProducts { get; set; } = new();
}

public class RevenueByDateAppDto
{
    public string DateLabel { get; set; } = "";
    public decimal Revenue { get; set; }
    public decimal Profit { get; set; }
}

public class TopSellingProductAppDto
{
    public string ProductName { get; set; } = "";
    public int QuantitySold { get; set; }
    public decimal Revenue { get; set; }
}
