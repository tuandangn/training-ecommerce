using NamEcommerce.Application.Contracts.Dtos.Report;
using NamEcommerce.Application.Contracts.Report;
using NamEcommerce.Domain.Entities.Catalog;
using NamEcommerce.Domain.Entities.DeliveryNotes;
using NamEcommerce.Domain.Entities.Finance;
using NamEcommerce.Domain.Entities.Returns;
using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Domain.Shared.Enums.DeliveryNotes;
using NamEcommerce.Domain.Shared.Enums.Inventory;
using NamEcommerce.Domain.Shared.Enums.Returns;
using NamEcommerce.Domain.Shared.Services.Inventory;

namespace NamEcommerce.Application.Services.Report;

public sealed class FinancialReportAppService : IFinancialReportAppService
{
    private readonly IEntityDataReader<DeliveryNote> _deliveryNoteReader;
    private readonly IEntityDataReader<CustomerReturn> _customerReturnReader;
    private readonly IEntityDataReader<Product> _productDataReader;
    private readonly IEntityDataReader<Expense> _expenseDataReader;
    private readonly IInventoryCostingManager _inventoryCostingManager;

    public FinancialReportAppService(
        IEntityDataReader<DeliveryNote> deliveryNoteReader,
        IEntityDataReader<CustomerReturn> customerReturnReader,
        IEntityDataReader<Product> productDataReader,
        IEntityDataReader<Expense> expenseDataReader,
        IInventoryCostingManager inventoryCostingManager)
    {
        _deliveryNoteReader = deliveryNoteReader;
        _customerReturnReader = customerReturnReader;
        _productDataReader = productDataReader;
        _expenseDataReader = expenseDataReader;
        _inventoryCostingManager = inventoryCostingManager;
    }

    public async Task<ProfitLossSummaryAppDto> GetProfitLossSummaryAsync(DateTime? fromDate, DateTime? toDate)
    {
        var fromUtc = fromDate?.ToUniversalTime();
        var toUtc = toDate.HasValue
            ? toDate.Value.Date.AddDays(1).AddTicks(-1).ToUniversalTime()
            : (DateTime?)null;

        var dnQuery = _deliveryNoteReader.DataSource
            .Where(dn => dn.Status == DeliveryNoteStatus.Delivered
                      && dn.SourceType == DeliveryNoteSourceType.ToCustomer
                      && dn.DeliveredOnUtc != null);

        if (fromUtc.HasValue) dnQuery = dnQuery.Where(dn => dn.DeliveredOnUtc >= fromUtc.Value);
        if (toUtc.HasValue) dnQuery = dnQuery.Where(dn => dn.DeliveredOnUtc <= toUtc.Value);

        var deliveredNotes = dnQuery
            .Select(dn => new
            {
                dn.Id,
                dn.DeliveredOnUtc,
                dn.TotalAmount,
                Items = dn.Items.Select(i => new
                {
                    i.ProductId,
                    i.Quantity,
                    i.UnitPrice,
                    i.SubTotal
                }).ToList()
            })
            .ToList();

        var crQuery = _customerReturnReader.DataSource
            .Where(cr => cr.Status == CustomerReturnStatus.Confirmed
                      && cr.ConfirmedOnUtc != null);

        if (fromUtc.HasValue) crQuery = crQuery.Where(cr => cr.ConfirmedOnUtc >= fromUtc.Value);
        if (toUtc.HasValue) crQuery = crQuery.Where(cr => cr.ConfirmedOnUtc <= toUtc.Value);

        var confirmedReturns = crQuery
            .Select(cr => new
            {
                cr.ConfirmedOnUtc,
                Items = cr.Items.Select(i => new { i.AcceptedQuantity, i.ReturnUnitPrice }).ToList()
            })
            .ToList();

        var productIds = deliveredNotes.SelectMany(dn => dn.Items).Select(i => i.ProductId).Distinct().ToList();
        var products = _productDataReader.DataSource.Where(p => productIds.Contains(p.Id)).ToList();
        var deliveryNoteIds = deliveredNotes.Select(dn => dn.Id).ToList();

        var cogsSummary = await _inventoryCostingManager
            .GetCogsForReferencesAsync(InventoryCostReferenceType.SalesOrder, deliveryNoteIds)
            .ConfigureAwait(false);

        var cogsByDeliveryNote = new Dictionary<Guid, decimal>();
        foreach (var deliveryNoteId in deliveryNoteIds)
        {
            var noteCogs = await _inventoryCostingManager
                .GetCogsForReferencesAsync(InventoryCostReferenceType.SalesOrder, [deliveryNoteId])
                .ConfigureAwait(false);
            cogsByDeliveryNote[deliveryNoteId] = noteCogs.TotalCost;
        }

        decimal grossRevenue = deliveredNotes.Sum(dn => dn.TotalAmount);
        decimal totalReturnAmt = confirmedReturns.Sum(cr => cr.Items.Sum(i => i.AcceptedQuantity * i.ReturnUnitPrice));

        var dto = new ProfitLossSummaryAppDto
        {
            GrossRevenue = grossRevenue,
            TotalReturnAmount = totalReturnAmt,
            TotalRevenue = grossRevenue - totalReturnAmt,
            TotalCogs = cogsSummary.TotalCost,
            HasPendingCogs = cogsSummary.HasPendingCost,
            HasRevaluedCogs = cogsSummary.HasRevaluedCost
        };

        var dateDict = new Dictionary<string, RevenueByDateAppDto>();
        var productDict = new Dictionary<Guid, TopSellingProductAppDto>();

        foreach (var dn in deliveredNotes)
        {
            var dateLabel = dn.DeliveredOnUtc!.Value.ToLocalTime().ToString("dd/MM/yyyy");
            if (!dateDict.TryGetValue(dateLabel, out var dayStats))
            {
                dayStats = new RevenueByDateAppDto { DateLabel = dateLabel };
                dateDict[dateLabel] = dayStats;
            }

            var dnCogs = cogsByDeliveryNote.GetValueOrDefault(dn.Id);
            dayStats.Revenue += dn.TotalAmount;
            dayStats.Profit += dn.TotalAmount - dnCogs;

            foreach (var item in dn.Items)
            {
                if (!productDict.TryGetValue(item.ProductId, out var topProd))
                {
                    var product = products.FirstOrDefault(p => p.Id == item.ProductId);
                    topProd = new TopSellingProductAppDto
                    {
                        ProductName = product?.Name ?? "(Không xác định)"
                    };
                    productDict[item.ProductId] = topProd;
                }

                topProd.QuantitySold += (int)item.Quantity;
                topProd.Revenue += item.SubTotal;
            }
        }

        foreach (var cr in confirmedReturns)
        {
            var returnAmt = cr.Items.Sum(i => i.AcceptedQuantity * i.ReturnUnitPrice);
            var dateLabel = cr.ConfirmedOnUtc!.Value.ToLocalTime().ToString("dd/MM/yyyy");
            if (dateDict.TryGetValue(dateLabel, out var dayStats))
            {
                dayStats.Revenue -= returnAmt;
                dayStats.Profit -= returnAmt;
            }
            else
            {
                dateDict[dateLabel] = new RevenueByDateAppDto
                {
                    DateLabel = dateLabel,
                    Revenue = -returnAmt,
                    Profit = -returnAmt
                };
            }
        }

        var expensesQuery = _expenseDataReader.DataSource;
        if (fromUtc.HasValue) expensesQuery = expensesQuery.Where(e => e.IncurredDate >= fromUtc.Value);
        if (toUtc.HasValue) expensesQuery = expensesQuery.Where(e => e.IncurredDate <= toUtc.Value);

        var expensesList = expensesQuery.Select(e => new { e.IncurredDate, e.Amount }).ToList();
        dto.TotalOperatingExpenses = expensesList.Sum(e => e.Amount);

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

        dto.RevenueTrend = dateDict.Values
            .OrderBy(x => DateTime.ParseExact(x.DateLabel, "dd/MM/yyyy", null))
            .ToList();

        dto.TopProducts = productDict.Values
            .OrderByDescending(x => x.QuantitySold)
            .Take(5)
            .ToList();

        return dto;
    }
}
