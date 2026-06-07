using NamEcommerce.Application.Contracts.Dtos.Finance;
using NamEcommerce.Application.Contracts.Finance;
using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Domain.Entities.Debts;
using NamEcommerce.Domain.Entities.DeliveryNotes;
using NamEcommerce.Domain.Entities.Finance;
using NamEcommerce.Domain.Entities.GoodsReceipts;
using NamEcommerce.Domain.Entities.Inventory;
using NamEcommerce.Domain.Shared.Enums.GoodsReceipts;
using NamEcommerce.Domain.Shared.Enums.Debts;
using NamEcommerce.Domain.Shared.Enums.DeliveryNotes;
using NamEcommerce.Domain.Shared.Enums.Finance;
using NamEcommerce.Domain.Shared.Enums.Inventory;

namespace NamEcommerce.Application.Services.Finance;

public sealed class AccountingReportService : IAccountingReportService
{
    private readonly IEntityDataReader<DeliveryNote> _deliveryNotes;
    private readonly IEntityDataReader<CustomerCreditNote> _creditNotes;
    private readonly IEntityDataReader<InventoryCostLedgerEntry> _costLedger;
    private readonly IEntityDataReader<Expense> _expenses;
    private readonly IEntityDataReader<FixedAsset> _fixedAssets;
    private readonly IEntityDataReader<CustomerDebt> _customerDebts;
    private readonly IEntityDataReader<VendorDebt> _vendorDebts;
    private readonly IEntityDataReader<VendorCreditNote> _vendorCreditNotes;
    private readonly IEntityDataReader<InventoryStock> _inventoryStocks;
    private readonly IEntityDataReader<GoodsReceipt> _goodsReceipts;
    private readonly IEntityDataReader<CustomerRefund> _refunds;
    private readonly IAccountingSetupAppService _setupService;
    private readonly ICashBookService _cashBookService;

    public AccountingReportService(
        IEntityDataReader<DeliveryNote> deliveryNotes,
        IEntityDataReader<CustomerCreditNote> creditNotes,
        IEntityDataReader<InventoryCostLedgerEntry> costLedger,
        IEntityDataReader<Expense> expenses,
        IEntityDataReader<FixedAsset> fixedAssets,
        IEntityDataReader<CustomerDebt> customerDebts,
        IEntityDataReader<VendorDebt> vendorDebts,
        IEntityDataReader<VendorCreditNote> vendorCreditNotes,
        IEntityDataReader<InventoryStock> inventoryStocks,
        IEntityDataReader<GoodsReceipt> goodsReceipts,
        IEntityDataReader<CustomerRefund> refunds,
        IAccountingSetupAppService setupService,
        ICashBookService cashBookService)
    {
        _deliveryNotes = deliveryNotes;
        _creditNotes = creditNotes;
        _costLedger = costLedger;
        _expenses = expenses;
        _fixedAssets = fixedAssets;
        _customerDebts = customerDebts;
        _vendorDebts = vendorDebts;
        _vendorCreditNotes = vendorCreditNotes;
        _inventoryStocks = inventoryStocks;
        _goodsReceipts = goodsReceipts;
        _refunds = refunds;
        _setupService = setupService;
        _cashBookService = cashBookService;
    }

    // ── B02 ───────────────────────────────────────────────────────────────────

    public async Task<IncomeStatementDto> GetIncomeStatementAsync(AccountingPeriod period)
    {
        var start = period.Start;
        var end = period.End;

        var allDeliveryNotes = _deliveryNotes.DataSource.ToList();

        var deliveredNoteIds = allDeliveryNotes
            .Where(dn => dn.Status == DeliveryNoteStatus.Delivered
                      && dn.DeliveredOnUtc >= start && dn.DeliveredOnUtc <= end)
            .Select(dn => dn.Id)
            .ToHashSet();

        var grossRevenue = allDeliveryNotes
            .Where(dn => deliveredNoteIds.Contains(dn.Id))
            .SelectMany(dn => dn.Items)
            .Sum(i => (decimal?)i.SubTotal) ?? 0;

        var tradeDiscounts = allDeliveryNotes
            .Where(dn => deliveredNoteIds.Contains(dn.Id))
            .SelectMany(dn => dn.Items)
            .Sum(i => (decimal?)i.DiscountAmount) ?? 0;

        var salesReturns = _creditNotes.DataSource
            .Where(cn => cn.CreatedOnUtc >= start && cn.CreatedOnUtc <= end
                      && cn.Status != CreditNoteStatus.Cancelled)
            .Sum(cn => (decimal?)cn.Amount) ?? 0;

        var cogs = _costLedger.DataSource
            .Where(e => e.MovementType == InventoryCostMovementType.SaleDispatch
                     && e.OccurredAtUtc >= start && e.OccurredAtUtc <= end)
            .Sum(e => (decimal?)e.TotalCost) ?? 0;

        var vendorReturnCost = _costLedger.DataSource
            .Where(e => e.MovementType == InventoryCostMovementType.VendorReturn
                     && e.OccurredAtUtc >= start && e.OccurredAtUtc <= end)
            .Sum(e => (decimal?)e.TotalCost) ?? 0;

        var sellingExp = _expenses.DataSource
            .Where(e => (e.ExpenseType == ExpenseType.Marketing || e.ExpenseType == ExpenseType.ReturnCost)
                     && e.IncurredDate >= start && e.IncurredDate <= end)
            .ToList()
            .Sum(e => (decimal?)e.AmountExcludingTax) ?? 0;

        var sellingDepreciation = _fixedAssets.DataSource
            .Where(a => a.CostCenter == FixedAssetCostCenter.Selling && a.Status != FixedAssetStatus.Disposed)
            .AsEnumerable()
            .Sum(a => SumDepreciationInPeriod(a, start, end));

        var adminExp = _expenses.DataSource
            .Where(e => (e.ExpenseType == ExpenseType.Payroll || e.ExpenseType == ExpenseType.Rent
                      || e.ExpenseType == ExpenseType.Utilities || e.ExpenseType == ExpenseType.General
                      || e.ExpenseType == ExpenseType.AssetDisposal)
                     && e.IncurredDate >= start && e.IncurredDate <= end)
            .ToList()
            .Sum(e => (decimal?)e.AmountExcludingTax) ?? 0;

        var adminDepreciation = _fixedAssets.DataSource
            .Where(a => a.CostCenter == FixedAssetCostCenter.Admin && a.Status != FixedAssetStatus.Disposed)
            .AsEnumerable()
            .Sum(a => SumDepreciationInPeriod(a, start, end));

        var setup = await _setupService.GetSetupAsync().ConfigureAwait(false);

        return new IncomeStatementDto
        {
            Period = period.Display,
            PeriodStart = start,
            PeriodEnd = end,
            GrossRevenue = grossRevenue,
            TradeDiscounts = tradeDiscounts,
            SalesReturns = salesReturns,
            CostOfGoodsSold = cogs,
            VendorReturnAdjustment = vendorReturnCost,
            SellingExpenses = sellingExp,
            SellingDepreciation = sellingDepreciation,
            AdminExpenses = adminExp,
            AdminDepreciation = adminDepreciation,
            CorporateTax = setup.CorporateTaxProvision ?? 0
        };
    }

    // ── B03 ───────────────────────────────────────────────────────────────────

    public async Task<CashFlowStatementDto> GetCashFlowStatementAsync(AccountingPeriod period)
    {
        var start = period.Start;
        var end = period.End;

        var b02 = await GetIncomeStatementAsync(period).ConfigureAwait(false);
        var totalDepreciation = b02.SellingDepreciation + b02.AdminDepreciation;

        var arEnd = GetNetAccountsReceivable(end);
        var arBegin = GetNetAccountsReceivable(start.AddDays(-1));
        var arChange = -(arEnd - arBegin);

        var apEnd = GetNetAccountsPayable(end);
        var apBegin = GetNetAccountsPayable(start.AddDays(-1));
        var apChange = apEnd - apBegin;

        var inventoryIncrease = _costLedger.DataSource
            .Where(e => (e.MovementType == InventoryCostMovementType.GoodsReceipt
                      || e.MovementType == InventoryCostMovementType.CustomerReturn
                      || e.MovementType == InventoryCostMovementType.VendorReturnReversal
                      || e.MovementType == InventoryCostMovementType.PositiveAdjustment
                      || e.MovementType == InventoryCostMovementType.TransferIn)
                     && e.OccurredAtUtc >= start && e.OccurredAtUtc <= end)
            .Sum(e => (decimal?)e.TotalCost) ?? 0;

        var inventoryDecrease = _costLedger.DataSource
            .Where(e => (e.MovementType == InventoryCostMovementType.SaleDispatch
                      || e.MovementType == InventoryCostMovementType.VendorReturn
                      || e.MovementType == InventoryCostMovementType.TransferOut
                      || e.MovementType == InventoryCostMovementType.NegativeAdjustment
                      || e.MovementType == InventoryCostMovementType.RevertReceipt)
                     && e.OccurredAtUtc >= start && e.OccurredAtUtc <= end)
            .Sum(e => (decimal?)e.TotalCost) ?? 0;

        var invChange = -(inventoryIncrease - inventoryDecrease);

        var vatEnd = GetVatPayable(end);
        var vatBegin = GetVatPayable(start.AddDays(-1));
        var vatChange = vatEnd - vatBegin;

        var refundsOut = -(_refunds.DataSource
            .Where(r => r.Status == CustomerRefundStatus.Completed
                     && r.RefundedOnUtc >= start && r.RefundedOnUtc <= end)
            .Sum(r => (decimal?)r.Amount) ?? 0);

        var assetPurchases = -(_fixedAssets.DataSource
            .Where(a => a.AcquisitionDate >= start && a.AcquisitionDate <= end)
            .Sum(a => (decimal?)a.AcquisitionCost) ?? 0);

        var setup = await _setupService.GetSetupAsync().ConfigureAwait(false);
        var openingCash = await GetOpeningCashForPeriodAsync(period, setup).ConfigureAwait(false);
        var actualClosingCash = await _cashBookService.GetTotalCashAsync(end).ConfigureAwait(false);

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

    // ── B01 ───────────────────────────────────────────────────────────────────

    public async Task<BalanceSheetDto> GetBalanceSheetAsync(DateTime asOf, bool includePriorPeriod = true)
    {
        var current = await BuildBalanceSheetAtDateAsync(asOf).ConfigureAwait(false);
        BalanceSheetDto? prior = null;

        if (includePriorPeriod)
        {
            var setup = await _setupService.GetSetupAsync().ConfigureAwait(false);
            var priorDate = setup.IsConfigured
                ? new DateTime(
                    setup.AccountingStartDate.Year == asOf.Year ? setup.AccountingStartDate.Year : asOf.Year - 1,
                    setup.FiscalYearStartMonth, setup.FiscalYearStartDay)
                : asOf.AddYears(-1);
            if (priorDate >= asOf) priorDate = asOf.AddYears(-1);
            prior = await BuildBalanceSheetAtDateAsync(priorDate).ConfigureAwait(false);
        }

        return current with { PriorPeriod = prior };
    }

    private async Task<BalanceSheetDto> BuildBalanceSheetAtDateAsync(DateTime asOf)
    {
        var setup = await _setupService.GetSetupAsync().ConfigureAwait(false);

        var cashOnHand = await _cashBookService.GetCashBalanceAsync(asOf).ConfigureAwait(false);
        var bankBalances = await _cashBookService.GetBankBalancesAsync(asOf).ConfigureAwait(false);

        var ar = GetNetAccountsReceivable(asOf);
        var inventory = _inventoryStocks.DataSource.Sum(s => s.QuantityOnHand * s.AverageCost);

        var grossAssets = _fixedAssets.DataSource
            .Where(a => a.Status != FixedAssetStatus.Disposed || (a.DisposedOnUtc.HasValue && a.DisposedOnUtc > asOf))
            .Sum(a => (decimal?)a.AcquisitionCost) ?? 0;

        var accDepreciation = _fixedAssets.DataSource
            .Where(a => a.Status != FixedAssetStatus.Disposed || (a.DisposedOnUtc.HasValue && a.DisposedOnUtc > asOf))
            .AsEnumerable()
            .Sum(a => a.GetAccumulatedDepreciation(asOf));

        var ap = GetNetAccountsPayable(asOf);
        var vatPayable = GetVatPayable(asOf);
        var citPayable = setup.IsConfigured ? (setup.CorporateTaxProvision ?? 0) : 0;
        var paidInCapital = setup.IsConfigured ? setup.OpeningEquity : 0;
        var retainedEarnings = setup.IsConfigured
            ? await ComputeCumulativeNetProfitAsync(setup.AccountingStartDate, asOf).ConfigureAwait(false)
            : 0;

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

    // ── Helpers ───────────────────────────────────────────────────────────────

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

    private decimal GetNetAccountsReceivable(DateTime asOf)
    {
        var gross = _customerDebts.DataSource
            .Where(d => d.CreatedOnUtc <= asOf
                     && (d.Status == DebtStatus.Outstanding || d.Status == DebtStatus.PartiallyPaid))
            .Sum(d => (decimal?)d.RemainingAmount) ?? 0;

        var creditNoteOffset = _creditNotes.DataSource
            .Where(cn => cn.CreatedOnUtc <= asOf
                      && (cn.Status == CreditNoteStatus.Unapplied || cn.Status == CreditNoteStatus.PartiallyApplied))
            .Sum(cn => (decimal?)cn.RemainingAmount) ?? 0;

        return gross - creditNoteOffset;
    }

    private decimal GetNetAccountsPayable(DateTime asOf)
    {
        var gross = _vendorDebts.DataSource
            .Where(d => d.CreatedOnUtc <= asOf
                     && (d.Status == DebtStatus.Outstanding || d.Status == DebtStatus.PartiallyPaid))
            .Sum(d => (decimal?)d.RemainingAmount) ?? 0;

        var creditNoteOffset = _vendorCreditNotes.DataSource
            .Where(cn => cn.CreatedOnUtc <= asOf
                      && (cn.Status == CreditNoteStatus.Unapplied || cn.Status == CreditNoteStatus.PartiallyApplied))
            .Sum(cn => (decimal?)cn.RemainingAmount) ?? 0;

        return gross - creditNoteOffset;
    }

    private decimal GetVatPayable(DateTime asOf)
    {
        var deliveredNoteIds = _deliveryNotes.DataSource
            .Where(dn => dn.Status == DeliveryNoteStatus.Delivered && dn.DeliveredOnUtc <= asOf)
            .Select(dn => dn.Id)
            .ToHashSet();

        var vatOut = _deliveryNotes.DataSource
            .Where(dn => deliveredNoteIds.Contains(dn.Id))
            .SelectMany(dn => dn.Items)
            .Sum(i => (decimal?)i.TaxAmount) ?? 0;

        var vatIn = _expenses.DataSource
            .Where(e => e.IncurredDate <= asOf)
            .Sum(e => (decimal?)e.TaxAmount) ?? 0;

        var vatInGoods = _goodsReceipts.DataSource
            .Where(gr => gr.ReceivedOnUtc <= asOf)
            .SelectMany(gr => gr.Items)
            .Sum(i => (decimal?)i.TaxAmount) ?? 0;

        return vatOut - vatIn - vatInGoods;
    }

    private async Task<decimal> ComputeCumulativeNetProfitAsync(DateTime from, DateTime to)
    {
        // Simplified: accumulate by year
        decimal total = 0;
        for (int year = from.Year; year <= to.Year; year++)
        {
            var b02 = await GetIncomeStatementAsync(AccountingPeriod.ForYear(year)).ConfigureAwait(false);
            total += b02.NetProfit;
        }
        return total;
    }

    private async Task<decimal> GetOpeningCashForPeriodAsync(AccountingPeriod period, AccountingSetupAppDto setup)
    {
        if (!setup.IsConfigured) return 0;
        var fiscalStart = new DateTime(period.Year, setup.FiscalYearStartMonth, setup.FiscalYearStartDay);
        if (period.Start <= fiscalStart)
            return setup.OpeningCash + (await _cashBookService.GetBankBalancesAsync(fiscalStart).ConfigureAwait(false)).Sum(b => b.Balance);
        return await _cashBookService.GetTotalCashAsync(period.Start.AddDays(-1)).ConfigureAwait(false);
    }
}
