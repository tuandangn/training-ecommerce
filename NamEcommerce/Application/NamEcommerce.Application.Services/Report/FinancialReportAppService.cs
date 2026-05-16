using NamEcommerce.Application.Contracts.Report;
using NamEcommerce.Application.Contracts.Dtos.Report;
using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Domain.Entities.Catalog;
using NamEcommerce.Domain.Entities.DeliveryNotes;
using NamEcommerce.Domain.Entities.Finance;
using NamEcommerce.Domain.Entities.Returns;
using NamEcommerce.Domain.Shared.Enums.DeliveryNotes;
using NamEcommerce.Domain.Shared.Enums.Returns;

namespace NamEcommerce.Application.Services.Report;

/// <summary>
/// Báo cáo tài chính dựa trên dòng xuất kho thực tế (DeliveryNote) thay vì đơn hàng tạo ra.
/// <list type="bullet">
///   <item>Doanh thu = tổng phiếu xuất (SourceType=ToCustomer, Status=Delivered) theo DeliveredOnUtc − trả hàng đã xác nhận.</item>
///   <item>COGS = tổng CostAtDispatch × Quantity của từng DeliveryNoteItem (giá vốn snapshot tại thời điểm xuất kho).</item>
/// </list>
/// </summary>
public sealed class FinancialReportAppService : IFinancialReportAppService
{
    private readonly IEntityDataReader<DeliveryNote> _deliveryNoteReader;
    private readonly IEntityDataReader<CustomerReturn> _customerReturnReader;
    private readonly IEntityDataReader<Product> _productDataReader;
    private readonly IEntityDataReader<Expense> _expenseDataReader;

    public FinancialReportAppService(
        IEntityDataReader<DeliveryNote> deliveryNoteReader,
        IEntityDataReader<CustomerReturn> customerReturnReader,
        IEntityDataReader<Product> productDataReader,
        IEntityDataReader<Expense> expenseDataReader)
    {
        _deliveryNoteReader = deliveryNoteReader;
        _customerReturnReader = customerReturnReader;
        _productDataReader = productDataReader;
        _expenseDataReader = expenseDataReader;
    }

    public Task<ProfitLossSummaryAppDto> GetProfitLossSummaryAsync(DateTime? fromDate, DateTime? toDate)
    {
        var fromUtc = fromDate?.ToUniversalTime();
        var toUtc = toDate.HasValue
            ? toDate.Value.Date.AddDays(1).AddTicks(-1).ToUniversalTime()
            : (DateTime?)null;

        // ── 1. Phiếu xuất kho cho khách (ToCustomer, Delivered) ────────────────
        var dnQuery = _deliveryNoteReader.DataSource
            .Where(dn => dn.Status == DeliveryNoteStatus.Delivered
                      && dn.SourceType == DeliveryNoteSourceType.ToCustomer
                      && dn.DeliveredOnUtc != null);

        if (fromUtc.HasValue) dnQuery = dnQuery.Where(dn => dn.DeliveredOnUtc >= fromUtc.Value);
        if (toUtc.HasValue) dnQuery = dnQuery.Where(dn => dn.DeliveredOnUtc <= toUtc.Value);

        var deliveredNotes = dnQuery
            .Select(dn => new
            {
                dn.DeliveredOnUtc,
                dn.TotalAmount,
                Items = dn.Items.Select(i => new
                {
                    i.ProductId,
                    i.Quantity,
                    i.UnitPrice,
                    i.SubTotal,
                    i.CostAtDispatch
                }).ToList()
            })
            .ToList();

        // ── 2. Trả hàng khách đã xác nhận (CustomerReturn, Confirmed) ──────────
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

        // ── 3. Tra cứu tên sản phẩm ────────────────────────────────────────────
        var productIds = deliveredNotes.SelectMany(dn => dn.Items).Select(i => i.ProductId).Distinct().ToList();
        var products = _productDataReader.DataSource.Where(p => productIds.Contains(p.Id)).ToList();

        // ── 4. Tính tổng ────────────────────────────────────────────────────────
        decimal grossRevenue = deliveredNotes.Sum(dn => dn.TotalAmount);
        decimal totalReturnAmt = confirmedReturns.Sum(cr => cr.Items.Sum(i => i.AcceptedQuantity * i.ReturnUnitPrice));
        decimal totalCogs = deliveredNotes
            .SelectMany(dn => dn.Items)
            .Sum(i => (i.CostAtDispatch ?? 0m) * i.Quantity);

        var dto = new ProfitLossSummaryAppDto
        {
            TotalRevenue = grossRevenue - totalReturnAmt,
            TotalCogs = totalCogs
        };

        // ── 5. Xu hướng doanh thu theo ngày ────────────────────────────────────
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

            decimal dnCogs = dn.Items.Sum(i => (i.CostAtDispatch ?? 0m) * i.Quantity);
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

        // Trừ giá trị trả hàng vào ngày xác nhận (giảm doanh thu + lợi nhuận ngày đó)
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

        // ── 6. Chi phí vận hành ────────────────────────────────────────────────
        var expensesQuery = _expenseDataReader.DataSource;
        if (fromUtc.HasValue) expensesQuery = expensesQuery.Where(e => e.IncurredDate >= fromUtc.Value);
        if (toUtc.HasValue) expensesQuery = expensesQuery.Where(e => e.IncurredDate <= toUtc.Value);

        var expensesList = expensesQuery.Select(e => new { e.IncurredDate, e.Amount }).ToList();
        dto.TotalOperatingExpenses = expensesList.Sum(e => e.Amount);

        foreach (var expense in expensesList)
        {
            var dateLabel = expense.IncurredDate.ToLocalTime().ToString("dd/MM/yyyy");
            if (dateDict.TryGetValue(dateLabel, out var dayStats))
                dayStats.Profit -= expense.Amount;
            else
                dateDict[dateLabel] = new RevenueByDateAppDto
                {
                    DateLabel = dateLabel,
                    Profit = -expense.Amount
                };
        }

        dto.RevenueTrend = dateDict.Values
            .OrderBy(x => DateTime.ParseExact(x.DateLabel, "dd/MM/yyyy", null))
            .ToList();

        dto.TopProducts = productDict.Values
            .OrderByDescending(x => x.QuantitySold)
            .Take(5)
            .ToList();

        return Task.FromResult(dto);
    }
}
