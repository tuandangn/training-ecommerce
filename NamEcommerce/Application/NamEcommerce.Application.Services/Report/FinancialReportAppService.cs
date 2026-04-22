using NamEcommerce.Application.Contracts.Report;
using NamEcommerce.Application.Contracts.Dtos.Report;
using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Domain.Entities.Orders;
using NamEcommerce.Domain.Entities.Catalog;
using NamEcommerce.Domain.Entities.Finance;
using NamEcommerce.Domain.Shared.Enums.Orders;

namespace NamEcommerce.Application.Services.Report;

public sealed class FinancialReportAppService : IFinancialReportAppService
{
    private readonly IEntityDataReader<Order> _orderDataReader;
    private readonly IEntityDataReader<Product> _productDataReader;
    private readonly IEntityDataReader<Expense> _expenseDataReader;

    public FinancialReportAppService(IEntityDataReader<Order> orderDataReader, IEntityDataReader<Product> productDataReader, IEntityDataReader<Expense> expenseDataReader)
    {
        _orderDataReader = orderDataReader;
        _productDataReader = productDataReader;
        _expenseDataReader = expenseDataReader;
    }

    public Task<ProfitLossSummaryAppDto> GetProfitLossSummaryAsync(DateTime? fromDate, DateTime? toDate)
    {
        var ordersQuery = _orderDataReader.DataSource;

        if (fromDate.HasValue)
            ordersQuery = ordersQuery.Where(o => o.CreatedOnUtc >= fromDate.Value.ToUniversalTime());

        if (toDate.HasValue)
        {
            var endOfDay = toDate.Value.Date.AddDays(1).AddTicks(-1).ToUniversalTime();
            ordersQuery = ordersQuery.Where(o => o.CreatedOnUtc <= endOfDay);
        }

        // Project to avoid N+1 and relationship load issues with EF Core
        var projectedOrders = ordersQuery.Select(o => new {
            o.Id,
            o.CreatedOnUtc,
            o.OrderTotal,
            Items = o.OrderItems.Select(i => new { i.ProductId, i.Quantity, i.SubTotal, i.CostPrice }).ToList()
        }).ToList();
        
        var dto = new ProfitLossSummaryAppDto();
        dto.TotalRevenue = projectedOrders.Sum(o => o.OrderTotal);
        
        decimal totalCogs = 0;
        var dateDict = new Dictionary<string, RevenueByDateAppDto>();
        var productDict = new Dictionary<Guid, TopSellingProductAppDto>();
        
        var productIds = projectedOrders.SelectMany(o => o.Items).Select(i => i.ProductId).Distinct().ToList();
        var products = _productDataReader.DataSource.Where(p => productIds.Contains(p.Id)).ToList();

        foreach (var order in projectedOrders)
        {
            var dateLabel = order.CreatedOnUtc.ToLocalTime().ToString("dd/MM/yyyy");
            if (!dateDict.ContainsKey(dateLabel))
                dateDict[dateLabel] = new RevenueByDateAppDto { DateLabel = dateLabel };

            var dayStats = dateDict[dateLabel];
            dayStats.Revenue += order.OrderTotal;
            
            decimal orderCogs = 0;
            foreach (var item in order.Items)
            {
                orderCogs += item.CostPrice * item.Quantity;
                
                if (!productDict.ContainsKey(item.ProductId))
                {
                    var product = products.FirstOrDefault(p => p.Id == item.ProductId);
                    productDict[item.ProductId] = new TopSellingProductAppDto 
                    { 
                        ProductName = product?.Name ?? "(Không xác định)"
                    };
                }
                
                var topProd = productDict[item.ProductId];
                topProd.QuantitySold += (int)item.Quantity;
                topProd.Revenue += item.SubTotal;
            }
            
            totalCogs += orderCogs;
            dayStats.Profit += (order.OrderTotal - orderCogs);
        }

        dto.TotalCogs = totalCogs;
        
        // Calculate Operating Expenses
        var expensesQuery = _expenseDataReader.DataSource;
        if (fromDate.HasValue)
            expensesQuery = expensesQuery.Where(e => e.IncurredDate >= fromDate.Value.ToUniversalTime());
        if (toDate.HasValue)
        {
            var endOfDay = toDate.Value.Date.AddDays(1).AddTicks(-1).ToUniversalTime();
            expensesQuery = expensesQuery.Where(e => e.IncurredDate <= endOfDay);
        }

        var expensesList = expensesQuery.Select(e => new { e.IncurredDate, e.Amount }).ToList();
        dto.TotalOperatingExpenses = expensesList.Sum(e => e.Amount);

        // Map daily expenses to profit
        foreach (var expense in expensesList)
        {
            var dateLabel = expense.IncurredDate.ToLocalTime().ToString("dd/MM/yyyy");
            if (dateDict.TryGetValue(dateLabel, out var dayStats))
            {
                dayStats.Profit -= expense.Amount;
            }
            else
            {
                dateDict[dateLabel] = new RevenueByDateAppDto 
                { 
                    DateLabel = dateLabel,
                    Profit = -expense.Amount
                };
            }
        }
        
        dto.RevenueTrend = dateDict.Values.OrderBy(x => DateTime.ParseExact(x.DateLabel, "dd/MM/yyyy", null)).ToList();
        dto.TopProducts = productDict.Values.OrderByDescending(x => x.QuantitySold).Take(5).ToList();

        return Task.FromResult(dto);
    }
}
