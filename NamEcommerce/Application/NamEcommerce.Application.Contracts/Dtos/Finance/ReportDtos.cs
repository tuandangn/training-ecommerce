namespace NamEcommerce.Application.Contracts.Dtos.Finance;

// ── B02 — Kết quả hoạt động kinh doanh ────────────────────────────────────────

public sealed record IncomeStatementDto
{
    public string Period { get; init; } = string.Empty;
    public DateTime PeriodStart { get; init; }
    public DateTime PeriodEnd { get; init; }

    public decimal GrossRevenue { get; init; }
    public decimal TradeDiscounts { get; init; }
    public decimal SalesReturns { get; init; }
    public decimal TotalDeductions => TradeDiscounts + SalesReturns;
    public decimal NetRevenue => GrossRevenue - TotalDeductions;

    public decimal CostOfGoodsSold { get; init; }
    public decimal VendorReturnAdjustment { get; init; }
    public decimal CustomerReturnAdjustment { get; init; }
    public decimal NetCostOfGoodsSold => CostOfGoodsSold - VendorReturnAdjustment - CustomerReturnAdjustment;

    public decimal GrossProfit => NetRevenue - NetCostOfGoodsSold;

    public decimal SellingExpenses { get; init; }
    public decimal SellingDepreciation { get; init; }
    public decimal TotalSellingExpenses => SellingExpenses + SellingDepreciation;

    public decimal AdminExpenses { get; init; }
    public decimal AdminDepreciation { get; init; }
    public decimal TotalAdminExpenses => AdminExpenses + AdminDepreciation;

    public decimal OperatingProfit => GrossProfit - TotalSellingExpenses - TotalAdminExpenses;
    public decimal ProfitBeforeTax => OperatingProfit;
    public decimal CorporateTax { get; init; }
    public decimal NetProfit => ProfitBeforeTax - CorporateTax;
}

// ── B03 — Lưu chuyển tiền tệ ──────────────────────────────────────────────────

public sealed record CashFlowStatementDto
{
    public string Period { get; init; } = string.Empty;
    public DateTime PeriodStart { get; init; }
    public DateTime PeriodEnd { get; init; }

    public decimal ProfitBeforeTax { get; init; }
    public decimal DepreciationAdjustment { get; init; }
    public decimal AccountsReceivableChange { get; init; }
    public decimal AccountsPayableChange { get; init; }
    public decimal InventoryChange { get; init; }
    public decimal VatPayableChange { get; init; }
    public decimal CustomerRefundsOut { get; init; }
    public decimal VendorRefundsIn { get; init; }
    public decimal OperatingCashFlow =>
        ProfitBeforeTax + DepreciationAdjustment
        + AccountsReceivableChange + AccountsPayableChange
        + InventoryChange + VatPayableChange
        + CustomerRefundsOut + VendorRefundsIn;

    public decimal FixedAssetPurchases { get; init; }
    public decimal InvestingCashFlow => FixedAssetPurchases;

    public decimal FinancingCashFlow => 0;

    public decimal NetCashChange => OperatingCashFlow + InvestingCashFlow + FinancingCashFlow;
    public decimal OpeningCash { get; init; }
    public decimal ClosingCash => OpeningCash + NetCashChange;
    public decimal ActualClosingCash { get; init; }
    public bool ClosingCashMatches => Math.Abs(ClosingCash - ActualClosingCash) < 1m;
}

// ── B01 — Bảng cân đối kế toán ────────────────────────────────────────────────

public sealed record BalanceSheetDto
{
    public DateTime AsOf { get; init; }
    public DateTime? PriorAsOf { get; init; }

    public decimal CashOnHand { get; init; }
    public IReadOnlyList<BankBalanceLineDto> BankDeposits { get; init; } = [];
    public decimal TotalBankDeposits => BankDeposits.Sum(b => b.Balance);
    public decimal TotalCash => CashOnHand + TotalBankDeposits;

    public decimal TradeReceivables { get; init; }
    public decimal Inventory { get; init; }
    public decimal TotalCurrentAssets => TotalCash + TradeReceivables + Inventory;

    public decimal FixedAssetsGross { get; init; }
    public decimal AccumulatedDepreciation { get; init; }
    public decimal FixedAssetsNet => FixedAssetsGross - AccumulatedDepreciation;
    public decimal TotalNonCurrentAssets => FixedAssetsNet;

    public decimal TotalAssets => TotalCurrentAssets + TotalNonCurrentAssets;

    public decimal TradePayables { get; init; }
    public decimal VatPayable { get; init; }
    public decimal CorporateTaxPayable { get; init; }
    public decimal TotalCurrentLiabilities => TradePayables + VatPayable + CorporateTaxPayable;
    public decimal TotalLiabilities => TotalCurrentLiabilities;

    public decimal PaidInCapital { get; init; }
    public decimal RetainedEarnings { get; init; }
    public decimal TotalEquity => PaidInCapital + RetainedEarnings;

    public decimal TotalLiabilitiesAndEquity => TotalLiabilities + TotalEquity;
    public bool IsBalanced => Math.Abs(TotalAssets - TotalLiabilitiesAndEquity) < 1m;
    public decimal Discrepancy => TotalAssets - TotalLiabilitiesAndEquity;

    public BalanceSheetDto? PriorPeriod { get; init; }
}

public sealed record BankBalanceLineDto
{
    public Guid BankAccountId { get; init; }
    public string DisplayName { get; init; } = string.Empty;
    public decimal Balance { get; init; }
}

// ── Sổ quỹ tiền mặt ───────────────────────────────────────────────────────────

public sealed record CashBookDto
{
    public string Period { get; init; } = string.Empty;
    public decimal OpeningBalance { get; init; }
    public decimal TotalIn { get; init; }
    public decimal TotalOut { get; init; }
    public decimal ClosingBalance => OpeningBalance + TotalIn - TotalOut;
    public IReadOnlyList<CashBookLineDto> Lines { get; init; } = [];
}

public sealed record CashBookLineDto
{
    public DateTime Date { get; init; }
    public string Description { get; init; } = string.Empty;
    public string SourceType { get; init; } = string.Empty;
    public Guid SourceId { get; init; }
    public decimal AmountIn { get; init; }
    public decimal AmountOut { get; init; }
    public decimal RunningBalance { get; init; }
}
