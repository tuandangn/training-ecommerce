# Implementation Spec: Report Service (Phase 2–5)

---

## Quyết định thiết kế

| Câu hỏi | Quyết định |
|---|---|
| Tổ chức service | 1 `IAccountingReportService` cho B02/B03/B01. `ICashBookService` riêng cho sổ quỹ. |
| Period handling | `AccountingPeriod` value object: chọn Month/Quarter/Year; convert → DateRange |
| B01 kỳ trước | B01 TT200 yêu cầu cột "Đầu kỳ" và "Cuối kỳ". "Đầu kỳ" = số dư tại `period.Start.AddDays(-1)` |
| Cân đối B01 | Nếu `TotalAssets != TotalLiabilitiesAndEquity` → log warning + trả về `IsBalanced = false` |
| Lazy load | Report service dùng trực tiếp `IEntityDataReader<T>.DataSource` (LINQ-to-SQL), không cache |
| COD payment | Nếu `CustomerPayment.BankAccountId == null && PaymentMethod == COD` → gộp vào TK mặc định khi tính sổ quỹ |

---

## 1. Shared Value Objects & Helpers

**File:** `NamEcommerce.Application.Contracts/Dtos/Finance/AccountingPeriod.cs`

```csharp
namespace NamEcommerce.Application.Contracts.Dtos.Finance;

/// <summary>Kỳ kế toán — chuyển đổi thành DateRange cho các query báo cáo.</summary>
[Serializable]
public sealed record AccountingPeriod
{
    public int Year { get; init; }
    public int? Month { get; init; }     // null = cả năm
    public int? Quarter { get; init; }   // 1–4; null nếu không phải quarter

    // Derived
    public DateTime Start => Month.HasValue
        ? new DateTime(Year, Month.Value, 1)
        : Quarter.HasValue
            ? new DateTime(Year, (Quarter.Value - 1) * 3 + 1, 1)
            : new DateTime(Year, 1, 1);

    public DateTime End => Month.HasValue
        ? Start.AddMonths(1).AddDays(-1)
        : Quarter.HasValue
            ? Start.AddMonths(3).AddDays(-1)
            : new DateTime(Year, 12, 31);

    public string Display => Month.HasValue ? $"Tháng {Month}/{Year}"
        : Quarter.HasValue ? $"Quý {Quarter}/{Year}"
        : $"Năm {Year}";

    public static AccountingPeriod ForMonth(int year, int month)
        => new() { Year = year, Month = month };
    public static AccountingPeriod ForQuarter(int year, int quarter)
        => new() { Year = year, Quarter = quarter };
    public static AccountingPeriod ForYear(int year)
        => new() { Year = year };
}
```

---

## 2. Report DTOs

**File:** `NamEcommerce.Application.Contracts/Dtos/Finance/ReportDtos.cs`

```csharp
namespace NamEcommerce.Application.Contracts.Dtos.Finance;

// ── B02 ─────────────────────────────────────────────────────────────────────

[Serializable]
public sealed record IncomeStatementDto
{
    public string Period { get; init; } = string.Empty;
    public DateTime PeriodStart { get; init; }
    public DateTime PeriodEnd { get; init; }

    // [01] Doanh thu gộp
    public decimal GrossRevenue { get; init; }

    // [02] Các khoản giảm trừ
    public decimal TradeDiscounts { get; init; }         // TK 521
    public decimal SalesReturns { get; init; }           // TK 531 (CustomerCreditNote)
    public decimal TotalDeductions => TradeDiscounts + SalesReturns;

    // [10] Doanh thu thuần
    public decimal NetRevenue => GrossRevenue - TotalDeductions;

    // [11] Giá vốn
    public decimal CostOfGoodsSold { get; init; }
    public decimal VendorReturnAdjustment { get; init; }  // âm
    public decimal NetCostOfGoodsSold => CostOfGoodsSold - VendorReturnAdjustment;

    // [20] Lợi nhuận gộp
    public decimal GrossProfit => NetRevenue - NetCostOfGoodsSold;

    // [25] Chi phí bán hàng
    public decimal SellingExpenses { get; init; }
    public decimal SellingDepreciation { get; init; }
    public decimal TotalSellingExpenses => SellingExpenses + SellingDepreciation;

    // [26] Chi phí QLDN
    public decimal AdminExpenses { get; init; }
    public decimal AdminDepreciation { get; init; }
    public decimal TotalAdminExpenses => AdminExpenses + AdminDepreciation;

    // [30] Lợi nhuận thuần từ HĐKD
    public decimal OperatingProfit => GrossProfit - TotalSellingExpenses - TotalAdminExpenses;

    // [50] LN trước thuế
    public decimal ProfitBeforeTax => OperatingProfit;

    // [51] Thuế TNDN
    public decimal CorporateTax { get; init; }

    // [60] LN sau thuế
    public decimal NetProfit => ProfitBeforeTax - CorporateTax;
}

// ── B03 ─────────────────────────────────────────────────────────────────────

[Serializable]
public sealed record CashFlowStatementDto
{
    public string Period { get; init; } = string.Empty;
    public DateTime PeriodStart { get; init; }
    public DateTime PeriodEnd { get; init; }

    // I. HĐKD
    public decimal ProfitBeforeTax { get; init; }
    public decimal DepreciationAdjustment { get; init; }     // cộng lại (phi tiền mặt)
    public decimal AccountsReceivableChange { get; init; }   // tăng AR → âm
    public decimal AccountsPayableChange { get; init; }      // tăng AP → dương
    public decimal InventoryChange { get; init; }            // tăng HTK → âm
    public decimal VatPayableChange { get; init; }
    public decimal CustomerRefundsOut { get; init; }         // âm
    public decimal OperatingCashFlow =>
        ProfitBeforeTax + DepreciationAdjustment
        + AccountsReceivableChange + AccountsPayableChange
        + InventoryChange + VatPayableChange
        + CustomerRefundsOut;

    // II. HĐ đầu tư
    public decimal FixedAssetPurchases { get; init; }        // âm
    public decimal InvestingCashFlow => FixedAssetPurchases;

    // III. HĐ tài chính
    public decimal FinancingCashFlow => 0;

    // IV–VI
    public decimal NetCashChange => OperatingCashFlow + InvestingCashFlow + FinancingCashFlow;
    public decimal OpeningCash { get; init; }
    public decimal ClosingCash => OpeningCash + NetCashChange;
    public decimal ActualClosingCash { get; init; }          // từ sổ quỹ — để verify
    public bool ClosingCashMatches => Math.Abs(ClosingCash - ActualClosingCash) < 1m;
}

// ── B01 ─────────────────────────────────────────────────────────────────────

[Serializable]
public sealed record BalanceSheetDto
{
    public DateTime AsOf { get; init; }
    public DateTime? PriorAsOf { get; init; }   // Đầu kỳ (AsOf - 1 day)

    // TÀI SẢN
    public decimal CashOnHand { get; init; }                    // TK 111
    public IReadOnlyList<BankBalanceLineDto> BankDeposits { get; init; } = [];
    public decimal TotalBankDeposits => BankDeposits.Sum(b => b.Balance);
    public decimal TotalCash => CashOnHand + TotalBankDeposits; // TK 111 + 112

    public decimal TradeReceivables { get; init; }              // TK 131
    public decimal Inventory { get; init; }                     // TK 156
    public decimal TotalCurrentAssets => TotalCash + TradeReceivables + Inventory;

    public decimal FixedAssetsGross { get; init; }              // TK 211
    public decimal AccumulatedDepreciation { get; init; }       // TK 214 (âm)
    public decimal FixedAssetsNet => FixedAssetsGross - AccumulatedDepreciation;
    public decimal TotalNonCurrentAssets => FixedAssetsNet;

    public decimal TotalAssets => TotalCurrentAssets + TotalNonCurrentAssets;

    // NGUỒN VỐN
    public decimal TradePayables { get; init; }                 // TK 331
    public decimal VatPayable { get; init; }                    // TK 3331
    public decimal CorporateTaxPayable { get; init; }           // TK 3334
    public decimal TotalCurrentLiabilities => TradePayables + VatPayable + CorporateTaxPayable;
    public decimal TotalLiabilities => TotalCurrentLiabilities;

    public decimal PaidInCapital { get; init; }                 // TK 411
    public decimal RetainedEarnings { get; init; }              // TK 421
    public decimal TotalEquity => PaidInCapital + RetainedEarnings;

    public decimal TotalLiabilitiesAndEquity => TotalLiabilities + TotalEquity;

    public bool IsBalanced => Math.Abs(TotalAssets - TotalLiabilitiesAndEquity) < 1m;
    public decimal Discrepancy => TotalAssets - TotalLiabilitiesAndEquity;

    // Cột so sánh đầu kỳ (nullable — chỉ có nếu PriorAsOf != null)
    public BalanceSheetDto? PriorPeriod { get; init; }
}

[Serializable]
public sealed record BankBalanceLineDto
{
    public Guid BankAccountId { get; init; }
    public string DisplayName { get; init; } = string.Empty;
    public decimal Balance { get; init; }
}

// ── Cash Book ────────────────────────────────────────────────────────────────

[Serializable]
public sealed record CashBookDto
{
    public string Period { get; init; } = string.Empty;
    public decimal OpeningBalance { get; init; }
    public decimal TotalIn { get; init; }
    public decimal TotalOut { get; init; }
    public decimal ClosingBalance => OpeningBalance + TotalIn - TotalOut;
    public IReadOnlyList<CashBookLineDto> Lines { get; init; } = [];
}

[Serializable]
public sealed record CashBookLineDto
{
    public DateTime Date { get; init; }
    public string Description { get; init; } = string.Empty;
    public string SourceType { get; init; } = string.Empty;  // "CustomerPayment" | "VendorPayment" | "Expense" | "CustomerRefund"
    public Guid SourceId { get; init; }
    public decimal AmountIn { get; init; }
    public decimal AmountOut { get; init; }
    public decimal RunningBalance { get; init; }
}
```

---

## 3. Service Interfaces

### 3.1 CashBook Service

**File:** `NamEcommerce.Application.Contracts/Finance/ICashBookService.cs`

```csharp
public interface ICashBookService
{
    /// <summary>Tính số dư tiền mặt (TK 111) đến thời điểm asOf.</summary>
    Task<decimal> GetCashBalanceAsync(DateTime asOf);

    /// <summary>Tính số dư từng TK ngân hàng đến asOf.</summary>
    Task<IReadOnlyList<BankBalanceLineDto>> GetBankBalancesAsync(DateTime asOf);

    /// <summary>Tổng TK 111 + TK 112.</summary>
    Task<decimal> GetTotalCashAsync(DateTime asOf);

    /// <summary>Sổ quỹ chi tiết (1 TK hoặc tất cả).</summary>
    Task<CashBookDto> GetCashBookAsync(AccountingPeriod period, Guid? bankAccountId = null);
}
```

### 3.2 Report Service

**File:** `NamEcommerce.Application.Contracts/Finance/IAccountingReportService.cs`

```csharp
public interface IAccountingReportService
{
    Task<IncomeStatementDto> GetIncomeStatementAsync(AccountingPeriod period);
    Task<CashFlowStatementDto> GetCashFlowStatementAsync(AccountingPeriod period);
    Task<BalanceSheetDto> GetBalanceSheetAsync(DateTime asOf, bool includePriorPeriod = true);
}
```

---

## 4. Service Implementations

### 4.1 CashBookService

**File:** `NamEcommerce.Application.Services/Finance/CashBookService.cs`

```csharp
public sealed class CashBookService : ICashBookService
{
    private readonly IEntityDataReader<CustomerPayment> _customerPayments;
    private readonly IEntityDataReader<VendorPayment> _vendorPayments;
    private readonly IEntityDataReader<Expense> _expenses;
    private readonly IEntityDataReader<CustomerRefund> _refunds;
    private readonly IEntityDataReader<BankAccount> _bankAccounts;
    private readonly IAccountingSetupAppService _setupService;

    // ... constructor

    public async Task<decimal> GetCashBalanceAsync(DateTime asOf)
    {
        var setup = await _setupService.GetSetupAsync();
        if (!setup.IsConfigured) return 0;

        var opening = setup.OpeningCash;

        var cashIn = _customerPayments.DataSource
            .Where(p => p.PaymentMethod == PaymentMethod.Cash && p.PaidOnUtc <= asOf)
            .Sum(p => (decimal?)p.Amount) ?? 0;

        var refundsOut = _refunds.DataSource
            .Where(r => r.PaymentMethod == PaymentMethod.Cash
                     && r.Status == CustomerRefundStatus.Completed
                     && r.RefundedOnUtc <= asOf)
            .Sum(r => (decimal?)r.Amount) ?? 0;

        var vendorOut = _vendorPayments.DataSource
            .Where(p => p.PaymentMethod == PaymentMethod.Cash && p.PaidOnUtc <= asOf)
            .Sum(p => (decimal?)p.Amount) ?? 0;

        var expensesOut = _expenses.DataSource
            .Where(e => e.PaymentMethod == PaymentMethod.Cash && e.IncurredDate <= asOf)
            .Sum(e => (decimal?)e.AmountExcludingTax) ?? 0;

        return opening + cashIn - refundsOut - vendorOut - expensesOut;
    }

    public async Task<IReadOnlyList<BankBalanceLineDto>> GetBankBalancesAsync(DateTime asOf)
    {
        var accounts = _bankAccounts.DataSource
            .Where(a => a.IsActive)
            .ToList();

        var result = new List<BankBalanceLineDto>();
        foreach (var account in accounts)
        {
            var balance = await GetBankAccountBalanceAsync(account.Id, account.OpeningBalance, asOf);
            result.Add(new BankBalanceLineDto
            {
                BankAccountId = account.Id,
                DisplayName = account.DisplayName,
                Balance = balance
            });
        }
        return result;
    }

    private Task<decimal> GetBankAccountBalanceAsync(Guid accountId, decimal openingBalance, DateTime asOf)
    {
        // PaymentMethod IN (BankTransfer, COD) với BankAccountId == accountId
        // COD không có BankAccountId → không tính vào TK cụ thể (hoặc gộp vào default)
        var cashIn = _customerPayments.DataSource
            .Where(p => p.BankAccountId == accountId && p.PaidOnUtc <= asOf)
            .Sum(p => (decimal?)p.Amount) ?? 0;

        var refundsOut = _refunds.DataSource
            .Where(r => r.BankAccountId == accountId
                     && r.Status == CustomerRefundStatus.Completed
                     && r.RefundedOnUtc <= asOf)
            .Sum(r => (decimal?)r.Amount) ?? 0;

        var vendorOut = _vendorPayments.DataSource
            .Where(p => p.BankAccountId == accountId && p.PaidOnUtc <= asOf)
            .Sum(p => (decimal?)p.Amount) ?? 0;

        var expensesOut = _expenses.DataSource
            .Where(e => e.BankAccountId == accountId && e.IncurredDate <= asOf)
            .Sum(e => (decimal?)e.AmountExcludingTax) ?? 0;

        return Task.FromResult(openingBalance + cashIn - refundsOut - vendorOut - expensesOut);
    }

    public async Task<decimal> GetTotalCashAsync(DateTime asOf)
    {
        var cash = await GetCashBalanceAsync(asOf);
        var banks = await GetBankBalancesAsync(asOf);
        return cash + banks.Sum(b => b.Balance);
    }

    public async Task<CashBookDto> GetCashBookAsync(AccountingPeriod period, Guid? bankAccountId = null)
    {
        // ... build lines từ CustomerPayment + VendorPayment + Expense + CustomerRefund
        // sort theo Date, tính RunningBalance lũy kế
        // nếu bankAccountId == null → tất cả (TK111 + TK112)
        // nếu bankAccountId == Guid.Empty → chỉ TK111 (tiền mặt)
        // nếu bankAccountId == specific → chỉ TK đó
        throw new NotImplementedException();
    }
}
```

---

### 4.2 AccountingReportService

**File:** `NamEcommerce.Application.Services/Finance/AccountingReportService.cs`

```csharp
public sealed class AccountingReportService : IAccountingReportService
{
    private readonly IEntityDataReader<DeliveryNote> _deliveryNotes;
    private readonly IEntityDataReader<DeliveryNoteItem> _deliveryNoteItems;
    private readonly IEntityDataReader<CustomerCreditNote> _creditNotes;
    private readonly IEntityDataReader<InventoryCostLedgerEntry> _costLedger;
    private readonly IEntityDataReader<VendorReturn> _vendorReturns;
    private readonly IEntityDataReader<Expense> _expenses;
    private readonly IEntityDataReader<FixedAsset> _fixedAssets;
    private readonly IEntityDataReader<CustomerDebt> _customerDebts;
    private readonly IEntityDataReader<VendorDebt> _vendorDebts;
    private readonly IEntityDataReader<VendorCreditNote> _vendorCreditNotes;
    private readonly IEntityDataReader<InventoryStock> _inventoryStocks;
    private readonly IAccountingSetupAppService _setupService;
    private readonly ICashBookService _cashBookService;

    // B02 ─────────────────────────────────────────────────────────────────

    public async Task<IncomeStatementDto> GetIncomeStatementAsync(AccountingPeriod period)
    {
        var start = period.Start;
        var end = period.End;

        // [01] Doanh thu gộp
        var grossRevenue = _deliveryNoteItems.DataSource
            .Where(i => i.DeliveryNote.Status == DeliveryNoteStatus.Delivered
                     && i.DeliveryNote.DeliveredOnUtc >= start
                     && i.DeliveryNote.DeliveredOnUtc <= end)
            .Sum(i => (decimal?)i.SubTotal) ?? 0;

        // [02a] Chiết khấu TM
        var tradeDiscounts = _deliveryNoteItems.DataSource
            .Where(i => i.DeliveryNote.Status == DeliveryNoteStatus.Delivered
                     && i.DeliveryNote.DeliveredOnUtc >= start
                     && i.DeliveryNote.DeliveredOnUtc <= end)
            .Sum(i => (decimal?)i.DiscountAmount) ?? 0;

        // [02b] Hàng trả lại (CustomerCreditNote tạo trong kỳ)
        var salesReturns = _creditNotes.DataSource
            .Where(cn => cn.CreatedOnUtc >= start && cn.CreatedOnUtc <= end
                      && cn.Status != CreditNoteStatus.Cancelled)
            .Sum(cn => (decimal?)cn.Amount) ?? 0;

        // [11] COGS
        var cogs = _costLedger.DataSource
            .Where(e => e.MovementType == InventoryCostMovementType.Dispatch
                     && e.CreatedOnUtc >= start && e.CreatedOnUtc <= end)
            .Sum(e => (decimal?)e.TotalCost) ?? 0;

        // Điều chỉnh VendorReturn (reduce COGS)
        // Lấy cost của items trong VendorReturn confirmed trong kỳ
        var vendorReturnAdj = 0m; // TODO: map VendorReturn items → cost via InventoryCostLedgerEntry

        // [25] Chi phí bán hàng
        var sellingExp = _expenses.DataSource
            .Where(e => e.ExpenseType is ExpenseType.Marketing or ExpenseType.ReturnCost
                     && e.IncurredDate >= start && e.IncurredDate <= end)
            .Sum(e => (decimal?)e.AmountExcludingTax) ?? 0;

        // [25] KH bán hàng (CostCenter = Selling)
        var sellingDepreciation = _fixedAssets.DataSource
            .Where(a => a.CostCenter == FixedAssetCostCenter.Selling && a.Status != FixedAssetStatus.Disposed)
            .AsEnumerable()
            .Sum(a => SumDepreciationInPeriod(a, start, end));

        // [26] Chi phí QLDN
        var adminExp = _expenses.DataSource
            .Where(e => e.ExpenseType is ExpenseType.Payroll or ExpenseType.Rent
                                      or ExpenseType.Utilities or ExpenseType.General
                     && e.IncurredDate >= start && e.IncurredDate <= end)
            .Sum(e => (decimal?)e.AmountExcludingTax) ?? 0;

        var adminDepreciation = _fixedAssets.DataSource
            .Where(a => a.CostCenter == FixedAssetCostCenter.Admin && a.Status != FixedAssetStatus.Disposed)
            .AsEnumerable()
            .Sum(a => SumDepreciationInPeriod(a, start, end));

        // [51] Thuế TNDN
        var setup = await _setupService.GetSetupAsync();
        var corporateTax = setup.CorporateTaxProvision ?? 0;

        return new IncomeStatementDto
        {
            Period = period.Display,
            PeriodStart = start,
            PeriodEnd = end,
            GrossRevenue = grossRevenue,
            TradeDiscounts = tradeDiscounts,
            SalesReturns = salesReturns,
            CostOfGoodsSold = cogs,
            VendorReturnAdjustment = vendorReturnAdj,
            SellingExpenses = sellingExp,
            SellingDepreciation = sellingDepreciation,
            AdminExpenses = adminExp,
            AdminDepreciation = adminDepreciation,
            CorporateTax = corporateTax
        };
    }

    // Helper: tổng KH của 1 TSCĐ trong khoảng start..end
    private static decimal SumDepreciationInPeriod(FixedAsset asset, DateTime start, DateTime end)
    {
        decimal total = 0;
        var current = new DateTime(start.Year, start.Month, 1);
        while (current <= end)
        {
            total += asset.GetDepreciationForMonth(current.Year, current.Month);
            current = current.AddMonths(1);
        }
        return total;
    }

    // B03 ─────────────────────────────────────────────────────────────────

    public async Task<CashFlowStatementDto> GetCashFlowStatementAsync(AccountingPeriod period)
    {
        var start = period.Start;
        var end = period.End;

        var b02 = await GetIncomeStatementAsync(period);

        // Khấu hao điều chỉnh = tổng KH kỳ này
        var totalDepreciation = b02.SellingDepreciation + b02.AdminDepreciation;

        // AR đầu kỳ / cuối kỳ
        var arEnd = GetNetAccountsReceivable(end);
        var arBegin = GetNetAccountsReceivable(start.AddDays(-1));
        var arChange = -(arEnd - arBegin);   // tăng AR → âm

        // AP đầu kỳ / cuối kỳ
        var apEnd = GetNetAccountsPayable(end);
        var apBegin = GetNetAccountsPayable(start.AddDays(-1));
        var apChange = apEnd - apBegin;      // tăng AP → dương

        // HTK đầu kỳ / cuối kỳ
        var invEnd = _inventoryStocks.DataSource.Sum(s => s.Quantity * s.AverageCost);
        // Lưu ý: inventory at start date cần snapshot — simplified: dùng cost ledger
        // TODO: tính inventory at start = invEnd - net movements in period
        var invBegin = 0m; // Placeholder — cần implement snapshot
        var invChange = -(invEnd - invBegin);

        // VAT payable change
        var vatEnd = GetVatPayable(end);
        var vatBegin = GetVatPayable(start.AddDays(-1));
        var vatChange = vatEnd - vatBegin;

        // Hoàn tiền KH
        var refundsOut = -(_refunds.DataSource
            .Where(r => r.Status == CustomerRefundStatus.Completed
                     && r.RefundedOnUtc >= start && r.RefundedOnUtc <= end)
            .Sum(r => (decimal?)r.Amount) ?? 0);

        // Mua TSCĐ trong kỳ
        var assetPurchases = -(_fixedAssets.DataSource
            .Where(a => a.AcquisitionDate >= start && a.AcquisitionDate <= end)
            .Sum(a => (decimal?)a.AcquisitionCost) ?? 0);

        var setup = await _setupService.GetSetupAsync();
        var openingCash = await GetOpeningCashForPeriod(period, setup);
        var actualClosingCash = await _cashBookService.GetTotalCashAsync(end);

        return new CashFlowStatementDto
        {
            Period = period.Display,
            PeriodStart = start,
            PeriodEnd = end,
            ProfitBeforeTax = b02.ProfitBeforeTax,
            DepreciationAdjustment = totalDepreciation,
            AccountsReceivableChange = arChange,
            AccountsPayableChange = apChange,
            InventoryChange = invChange,
            VatPayableChange = vatChange,
            CustomerRefundsOut = refundsOut,
            FixedAssetPurchases = assetPurchases,
            OpeningCash = openingCash,
            ActualClosingCash = actualClosingCash
        };
    }

    // B01 ─────────────────────────────────────────────────────────────────

    public async Task<BalanceSheetDto> GetBalanceSheetAsync(DateTime asOf, bool includePriorPeriod = true)
    {
        var current = await BuildBalanceSheetAtDateAsync(asOf);
        BalanceSheetDto? prior = null;

        if (includePriorPeriod)
        {
            // Đầu kỳ = 1 năm trước (hoặc AccountingStartDate nếu < 1 năm)
            var setup = await _setupService.GetSetupAsync();
            var priorDate = setup.IsConfigured
                ? new DateTime(setup.AccountingStartDate.Year == asOf.Year
                    ? setup.AccountingStartDate.Year
                    : asOf.Year - 1,
                    setup.FiscalYearStartMonth, setup.FiscalYearStartDay)
                : asOf.AddYears(-1);
            priorDate = priorDate < asOf ? priorDate : asOf.AddYears(-1);
            prior = await BuildBalanceSheetAtDateAsync(priorDate);
        }

        return current with { PriorPeriod = prior };
    }

    private async Task<BalanceSheetDto> BuildBalanceSheetAtDateAsync(DateTime asOf)
    {
        var setup = await _setupService.GetSetupAsync();
        var accStartDate = setup.IsConfigured ? setup.AccountingStartDate : DateTime.MinValue;

        // TK 111
        var cashOnHand = await _cashBookService.GetCashBalanceAsync(asOf);

        // TK 112
        var bankBalances = await _cashBookService.GetBankBalancesAsync(asOf);

        // TK 131 — phải thu KH (loại trừ credit notes chưa apply)
        var ar = GetNetAccountsReceivable(asOf);

        // TK 156 — hàng tồn kho
        var inventory = _inventoryStocks.DataSource
            .Sum(s => s.Quantity * s.AverageCost);

        // TK 211 — nguyên giá TSCĐ
        var grossAssets = _fixedAssets.DataSource
            .Where(a => a.Status != FixedAssetStatus.Disposed || a.DisposedOnUtc > asOf)
            .Sum(a => (decimal?)a.AcquisitionCost) ?? 0;

        // TK 214 — khấu hao lũy kế
        var accDepreciation = _fixedAssets.DataSource
            .Where(a => a.Status != FixedAssetStatus.Disposed || a.DisposedOnUtc > asOf)
            .AsEnumerable()
            .Sum(a => a.GetAccumulatedDepreciation(asOf));

        // TK 331 — phải trả NCC
        var ap = GetNetAccountsPayable(asOf);

        // TK 3331 — thuế GTGT phải nộp (lũy kế từ AccountingStartDate)
        var vatPayable = GetVatPayable(asOf);

        // TK 3334 — thuế TNDN
        var citPayable = setup.IsConfigured ? (setup.CorporateTaxProvision ?? 0) : 0;

        // TK 411 — vốn góp
        var paidInCapital = setup.IsConfigured ? setup.OpeningEquity : 0;

        // TK 421 — LNST lũy kế (tính tất cả các kỳ từ AccStartDate đến asOf)
        // = GrossRevenue - Deductions - COGS - Expenses - Tax (lũy kế)
        // Simplified: dùng report với period = AccStartDate..asOf
        var retainedEarnings = await ComputeCumulativeNetProfitAsync(accStartDate, asOf);

        return new BalanceSheetDto
        {
            AsOf = asOf,
            CashOnHand = cashOnHand,
            BankDeposits = bankBalances,
            TradeReceivables = ar,
            Inventory = inventory,
            FixedAssetsGross = grossAssets,
            AccumulatedDepreciation = accDepreciation,
            TradePayables = ap,
            VatPayable = vatPayable,
            CorporateTaxPayable = citPayable,
            PaidInCapital = paidInCapital,
            RetainedEarnings = retainedEarnings
        };
    }

    // ── Helpers ──────────────────────────────────────────────────────────

    private decimal GetNetAccountsReceivable(DateTime asOf)
    {
        var gross = _customerDebts.DataSource
            .Where(d => d.CreatedOnUtc <= asOf
                     && d.Status is DebtStatus.Outstanding or DebtStatus.PartiallyPaid)
            .Sum(d => (decimal?)d.RemainingAmount) ?? 0;

        var creditNoteOffset = _creditNotes.DataSource
            .Where(cn => cn.CreatedOnUtc <= asOf
                      && cn.Status is CreditNoteStatus.Unapplied or CreditNoteStatus.PartiallyApplied)
            .Sum(cn => (decimal?)cn.RemainingAmount) ?? 0;

        return gross - creditNoteOffset;
    }

    private decimal GetNetAccountsPayable(DateTime asOf)
    {
        var gross = _vendorDebts.DataSource
            .Where(d => d.CreatedOnUtc <= asOf
                     && d.Status is DebtStatus.Outstanding or DebtStatus.PartiallyPaid)
            .Sum(d => (decimal?)d.RemainingAmount) ?? 0;

        var creditNoteOffset = _vendorCreditNotes.DataSource
            .Where(cn => cn.CreatedOnUtc <= asOf
                      && cn.Status is CreditNoteStatus.Unapplied or CreditNoteStatus.PartiallyApplied)
            .Sum(cn => (decimal?)cn.RemainingAmount) ?? 0;

        return gross - creditNoteOffset;
    }

    private decimal GetVatPayable(DateTime asOf)
    {
        var vatOut = _deliveryNoteItems.DataSource
            .Where(i => i.DeliveryNote.Status == DeliveryNoteStatus.Delivered
                     && i.DeliveryNote.DeliveredOnUtc <= asOf)
            .Sum(i => (decimal?)i.TaxAmount) ?? 0;

        var vatIn = _expenses.DataSource
            .Where(e => e.IncurredDate <= asOf)
            .Sum(e => (decimal?)e.TaxAmount) ?? 0;

        var vatInGoods = _goodsReceiptItems.DataSource
            .Where(i => i.GoodsReceipt.ReceivedOnUtc <= asOf)
            .Sum(i => (decimal?)i.TaxAmount) ?? 0;

        return vatOut - vatIn - vatInGoods;
    }

    private async Task<decimal> ComputeCumulativeNetProfitAsync(DateTime from, DateTime to)
    {
        // Dùng period = from..to
        var period = new AccountingPeriod
        {
            Year = to.Year,
            Month = null,
            Quarter = null
        };
        // Cần custom period không theo Month/Quarter/Year:
        // Tạm thời: tính theo năm tài chính
        // TODO: refactor period để hỗ trợ arbitrary date range
        var b02 = await GetIncomeStatementAsync(AccountingPeriod.ForYear(to.Year));
        return b02.NetProfit;
    }

    private async Task<decimal> GetOpeningCashForPeriod(AccountingPeriod period, AccountingSetupAppDto setup)
    {
        if (!setup.IsConfigured) return 0;
        var fiscalStart = new DateTime(period.Year, setup.FiscalYearStartMonth, setup.FiscalYearStartDay);
        if (period.Start <= fiscalStart)
            return setup.OpeningCash + (await _cashBookService.GetBankBalancesAsync(fiscalStart)).Sum(b => b.Balance);
        return await _cashBookService.GetTotalCashAsync(period.Start.AddDays(-1));
    }
}
```

---

## 5. Presentation Layer

### 5.1 Queries

```csharp
public sealed class GetIncomeStatementQuery : IRequest<IncomeStatementModel>
{
    public int Year { get; init; }
    public int? Month { get; init; }
    public int? Quarter { get; init; }
}

public sealed class GetCashFlowQuery : IRequest<CashFlowModel>
{
    public int Year { get; init; }
    public int? Month { get; init; }
    public int? Quarter { get; init; }
}

public sealed class GetBalanceSheetQuery : IRequest<BalanceSheetModel>
{
    public DateTime AsOf { get; init; }
}

public sealed class GetCashBookQuery : IRequest<CashBookModel>
{
    public int Year { get; init; }
    public int? Month { get; init; }
    public int? Quarter { get; init; }
    public Guid? BankAccountId { get; init; }
}
```

---

### 5.2 Controller

```csharp
// GET /Accounting/CashBook?year=2024&month=3
[HttpGet("CashBook")]
public async Task<IActionResult> CashBook(int year, int? month, int? quarter, Guid? bankAccountId)
    => View(await _mediator.Send(new GetCashBookQuery { Year = year, Month = month, Quarter = quarter, BankAccountId = bankAccountId }));

// GET /Accounting/IncomeStatement?year=2024&month=3
[HttpGet("IncomeStatement")]
public async Task<IActionResult> IncomeStatement(int year, int? month, int? quarter)
    => View(await _mediator.Send(new GetIncomeStatementQuery { Year = year, Month = month, Quarter = quarter }));

// GET /Accounting/CashFlow?year=2024&month=3
[HttpGet("CashFlow")]
public async Task<IActionResult> CashFlow(int year, int? month, int? quarter)
    => View(await _mediator.Send(new GetCashFlowQuery { Year = year, Month = month, Quarter = quarter }));

// GET /Accounting/BalanceSheet?asOf=2024-03-31
[HttpGet("BalanceSheet")]
public async Task<IActionResult> BalanceSheet(DateTime? asOf)
    => View(await _mediator.Send(new GetBalanceSheetQuery { AsOf = asOf ?? DateTime.Today }));
```

---

### 5.3 View Specs

**Common layout cho tất cả báo cáo:**
```
Header: [Tên báo cáo] — [Tên công ty] — [Kỳ/Thời điểm]
Period picker: [Tháng] [Quý] [Năm] — dropdowns, submit = GET
Nút: [In] [Xuất Excel (Phase sau)]
@media print: ẩn sidebar, nav, period picker; hiển thị đầy đủ table
```

**B02 IncomeStatement:**
```
Table 2 cột: CHỈ TIÊU | SỐ TIỀN
Dòng tổng (Net Revenue, Gross Profit, Operating Profit, Net Profit) in đậm
Dòng sub-total thụt lề
```

**B03 CashFlow:**
```
Table 2 cột: CHỈ TIÊU | SỐ TIỀN
Phần I, II, III làm section header
Dòng kiểm tra: "Tiền cuối kỳ B03: X | Sổ quỹ: Y" — highlight đỏ nếu không khớp
```

**B01 BalanceSheet:**
```
Table 3 cột: CHỈ TIÊU | CUỐI KỲ | ĐẦU KỲ
Phần TÀI SẢN / NGUỒN VỐN là section headers
Dòng cuối: "TỔNG TÀI SẢN = TỔNG NGUỒN VỐN"
Alert đỏ nếu IsBalanced = false, hiển thị Discrepancy
```

**CashBook:**
```
Summary cards: Tiền mặt | Tổng ngân hàng (per TK) | Tổng
Filter: Kỳ | Tài khoản (Tất cả / TK111 / từng NH)
Table: Ngày | Diễn giải | Vào | Ra | Số dư
```
