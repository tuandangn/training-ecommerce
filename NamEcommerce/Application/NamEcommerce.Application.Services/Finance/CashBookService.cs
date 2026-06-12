using NamEcommerce.Application.Contracts.Dtos.Finance;
using NamEcommerce.Application.Contracts.Finance;
using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Domain.Entities.Debts;
using NamEcommerce.Domain.Entities.Finance;
using NamEcommerce.Domain.Shared.Enums.Debts;
using NamEcommerce.Domain.Shared.Enums.Orders;
using NamEcommerce.Data.Contracts;

namespace NamEcommerce.Application.Services.Finance;

public sealed class CashBookService : ICashBookService
{
    private readonly IEntityDataReader<CustomerPayment> _customerPayments;
    private readonly IEntityDataReader<VendorPayment> _vendorPayments;
    private readonly IEntityDataReader<Expense> _expenses;
    private readonly IEntityDataReader<CustomerRefund> _refunds;
    private readonly IEntityDataReader<VendorRefund> _vendorRefunds;
    private readonly IEntityDataReader<BankAccount> _bankAccounts;
    private readonly IAccountingSetupAppService _setupService;

    public CashBookService(
        IEntityDataReader<CustomerPayment> customerPayments,
        IEntityDataReader<VendorPayment> vendorPayments,
        IEntityDataReader<Expense> expenses,
        IEntityDataReader<CustomerRefund> refunds,
        IEntityDataReader<VendorRefund> vendorRefunds,
        IEntityDataReader<BankAccount> bankAccounts,
        IAccountingSetupAppService setupService)
    {
        _customerPayments = customerPayments;
        _vendorPayments = vendorPayments;
        _expenses = expenses;
        _refunds = refunds;
        _vendorRefunds = vendorRefunds;
        _bankAccounts = bankAccounts;
        _setupService = setupService;
    }

    public async Task<decimal> GetCashBalanceAsync(DateTime asOf)
    {
        var setup = await _setupService.GetSetupAsync().ConfigureAwait(false);
        if (!setup.IsConfigured) return 0;

        var cutoff = asOf.Date.AddDays(1).AddTicks(-1);

        var cashIn = _customerPayments.DataSource
            .Where(p => p.PaymentMethod == PaymentMethod.Cash && p.PaidOnUtc <= cutoff)
            .Sum(p => (decimal?)p.Amount) ?? 0;

        var refundsOut = _refunds.DataSource
            .Where(r => r.PaymentMethod == PaymentMethod.Cash
                     && r.Status == CustomerRefundStatus.Completed
                     && r.RefundedOnUtc <= cutoff)
            .Sum(r => (decimal?)r.Amount) ?? 0;

        var vendorRefundsIn = _vendorRefunds.DataSource
            .Where(r => r.PaymentMethod == PaymentMethod.Cash
                     && r.Status == VendorRefundStatus.Completed
                     && r.RefundedOnUtc <= cutoff)
            .Sum(r => (decimal?)r.Amount) ?? 0;

        var vendorOut = _vendorPayments.DataSource
            .Where(p => p.PaymentMethod == PaymentMethod.Cash && p.PaidOnUtc <= cutoff)
            .Sum(p => (decimal?)p.Amount) ?? 0;

        var expensesOut = _expenses.DataSource
            .Where(e => (e.PaymentMethod == null || e.PaymentMethod == PaymentMethod.Cash)
                     && e.IncurredDate <= cutoff)
            .ToList()
            .Sum(e => (decimal?)e.AmountExcludingTax) ?? 0;

        return setup.OpeningCash + cashIn + vendorRefundsIn - refundsOut - vendorOut - expensesOut;
    }

    public async Task<IReadOnlyList<BankBalanceLineDto>> GetBankBalancesAsync(DateTime asOf)
    {
        var accounts = _bankAccounts.DataSource.Where(a => a.IsActive).ToList();
        var result = new List<BankBalanceLineDto>(accounts.Count);
        foreach (var account in accounts)
        {
            var balance = GetBankAccountBalance(account.Id, account.OpeningBalance, asOf);
            result.Add(new BankBalanceLineDto
            {
                BankAccountId = account.Id,
                DisplayName = account.DisplayName,
                Balance = balance
            });
        }
        return await Task.FromResult<IReadOnlyList<BankBalanceLineDto>>(result);
    }

    public async Task<decimal> GetTotalCashAsync(DateTime asOf)
    {
        var cash = await GetCashBalanceAsync(asOf).ConfigureAwait(false);
        var banks = await GetBankBalancesAsync(asOf).ConfigureAwait(false);
        return cash + banks.Sum(b => b.Balance);
    }

    public async Task<CashBookDto> GetCashBookAsync(AccountingPeriod period, Guid? bankAccountId = null)
    {
        var start = period.Start;
        var end = period.End;

        // Opening balance = balance at end of previous day
        var openingDate = start.AddDays(-1);
        decimal openingBalance;
        if (bankAccountId == null)
            openingBalance = await GetTotalCashAsync(openingDate).ConfigureAwait(false);
        else if (bankAccountId == Guid.Empty)
            openingBalance = await GetCashBalanceAsync(openingDate).ConfigureAwait(false);
        else
        {
            var account = _bankAccounts.DataSource.FirstOrDefault(a => a.Id == bankAccountId.Value);
            openingBalance = account is null ? 0 : GetBankAccountBalance(account.Id, account.OpeningBalance, openingDate);
        }

        var lines = BuildLines(start, end, bankAccountId);
        var sorted = lines.OrderBy(l => l.Date).ThenBy(l => l.SourceType).ToList();

        decimal running = openingBalance;
        var resultLines = new List<CashBookLineDto>(sorted.Count);
        foreach (var line in sorted)
        {
            running += line.AmountIn - line.AmountOut;
            resultLines.Add(line with { RunningBalance = running });
        }

        return new CashBookDto
        {
            Period = period.Display,
            OpeningBalance = openingBalance,
            TotalIn = resultLines.Sum(l => l.AmountIn),
            TotalOut = resultLines.Sum(l => l.AmountOut),
            Lines = resultLines
        };
    }

    private decimal GetBankAccountBalance(Guid accountId, decimal openingBalance, DateTime asOf)
    {
        var cutoff = asOf.Date.AddDays(1).AddTicks(-1);
        var fallbackBankAccountId = GetFallbackBankAccountId();

        var cashIn = _customerPayments.DataSource
            .Where(p => (p.BankAccountId == accountId
                     || (p.BankAccountId == null && p.PaymentMethod == PaymentMethod.BankTransfer && fallbackBankAccountId == accountId))
                     && p.PaidOnUtc <= cutoff)
            .Sum(p => (decimal?)p.Amount) ?? 0;

        var refundsOut = _refunds.DataSource
            .Where(r => (r.BankAccountId == accountId
                     || (r.BankAccountId == null && r.PaymentMethod == PaymentMethod.BankTransfer && fallbackBankAccountId == accountId))
                     && r.Status == CustomerRefundStatus.Completed
                     && r.RefundedOnUtc <= cutoff)
            .Sum(r => (decimal?)r.Amount) ?? 0;

        var vendorRefundsIn = _vendorRefunds.DataSource
            .Where(r => (r.BankAccountId == accountId
                     || (r.BankAccountId == null && r.PaymentMethod == PaymentMethod.BankTransfer && fallbackBankAccountId == accountId))
                     && r.Status == VendorRefundStatus.Completed
                     && r.RefundedOnUtc <= cutoff)
            .Sum(r => (decimal?)r.Amount) ?? 0;

        var vendorOut = _vendorPayments.DataSource
            .Where(p => (p.BankAccountId == accountId
                     || (p.BankAccountId == null && p.PaymentMethod == PaymentMethod.BankTransfer && fallbackBankAccountId == accountId))
                     && p.PaidOnUtc <= cutoff)
            .Sum(p => (decimal?)p.Amount) ?? 0;

        var expensesOut = _expenses.DataSource
            .Where(e => (e.BankAccountId == accountId
                     || (e.BankAccountId == null && e.PaymentMethod == PaymentMethod.BankTransfer && fallbackBankAccountId == accountId))
                     && e.IncurredDate <= cutoff)
            .ToList()
            .Sum(e => (decimal?)e.AmountExcludingTax) ?? 0;

        return openingBalance + cashIn + vendorRefundsIn - refundsOut - vendorOut - expensesOut;
    }

    private List<CashBookLineDto> BuildLines(DateTime start, DateTime end, Guid? bankAccountId)
    {
        var lines = new List<CashBookLineDto>();
        bool isCash = bankAccountId == Guid.Empty;
        bool isAll = bankAccountId == null;
        var fallbackBankAccountId = GetFallbackBankAccountId();

        // CustomerPayment (tiền vào)
        var cpQuery = _customerPayments.DataSource
            .Where(p => p.PaidOnUtc >= start && p.PaidOnUtc <= end);
        if (isCash) cpQuery = cpQuery.Where(p => p.PaymentMethod == PaymentMethod.Cash);
        else if (!isAll) cpQuery = cpQuery.Where(p => p.BankAccountId == bankAccountId!.Value
            || (p.BankAccountId == null && p.PaymentMethod == PaymentMethod.BankTransfer && fallbackBankAccountId == bankAccountId.Value));
        foreach (var p in cpQuery.ToList())
            lines.Add(new CashBookLineDto { Date = p.PaidOnUtc, Description = $"Thu KH: {p.CustomerName}", SourceType = "CustomerPayment", SourceId = p.Id, AmountIn = p.Amount });

        // CustomerRefund (tiền ra)
        var refQuery = _refunds.DataSource
            .Where(r => r.Status == CustomerRefundStatus.Completed
                     && r.RefundedOnUtc >= start && r.RefundedOnUtc <= end);
        if (isCash) refQuery = refQuery.Where(r => r.PaymentMethod == PaymentMethod.Cash);
        else if (!isAll) refQuery = refQuery.Where(r => r.BankAccountId == bankAccountId!.Value
            || (r.BankAccountId == null && r.PaymentMethod == PaymentMethod.BankTransfer && fallbackBankAccountId == bankAccountId.Value));
        foreach (var r in refQuery.ToList())
            lines.Add(new CashBookLineDto { Date = r.RefundedOnUtc!.Value, Description = $"Hoàn tiền KH: {r.CustomerName}", SourceType = "CustomerRefund", SourceId = r.Id, AmountOut = r.Amount });

        // VendorRefund (tiền vào — NCC hoàn tiền cho shop)
        var vrQuery = _vendorRefunds.DataSource
            .Where(r => r.Status == VendorRefundStatus.Completed
                     && r.RefundedOnUtc >= start && r.RefundedOnUtc <= end);
        if (isCash) vrQuery = vrQuery.Where(r => r.PaymentMethod == PaymentMethod.Cash);
        else if (!isAll) vrQuery = vrQuery.Where(r => r.BankAccountId == bankAccountId!.Value
            || (r.BankAccountId == null && r.PaymentMethod == PaymentMethod.BankTransfer && fallbackBankAccountId == bankAccountId.Value));
        foreach (var r in vrQuery.ToList())
            lines.Add(new CashBookLineDto { Date = r.RefundedOnUtc!.Value, Description = $"NCC hoàn tiền: {r.VendorName}", SourceType = "VendorRefund", SourceId = r.Id, AmountIn = r.Amount });

        // VendorPayment (tiền ra)
        var vpQuery = _vendorPayments.DataSource
            .Where(p => p.PaidOnUtc >= start && p.PaidOnUtc <= end);
        if (isCash) vpQuery = vpQuery.Where(p => p.PaymentMethod == PaymentMethod.Cash);
        else if (!isAll) vpQuery = vpQuery.Where(p => p.BankAccountId == bankAccountId!.Value
            || (p.BankAccountId == null && p.PaymentMethod == PaymentMethod.BankTransfer && fallbackBankAccountId == bankAccountId.Value));
        foreach (var p in vpQuery.ToList())
            lines.Add(new CashBookLineDto { Date = p.PaidOnUtc, Description = $"Trả NCC: {p.VendorName}", SourceType = "VendorPayment", SourceId = p.Id, AmountOut = p.Amount });

        // Expense (tiền ra)
        var expQuery = _expenses.DataSource
            .Where(e => e.IncurredDate >= start && e.IncurredDate <= end);
        if (isCash) expQuery = expQuery.Where(e => e.PaymentMethod == null || e.PaymentMethod == PaymentMethod.Cash);
        else if (!isAll) expQuery = expQuery.Where(e => e.BankAccountId == bankAccountId!.Value
            || (e.BankAccountId == null && e.PaymentMethod == PaymentMethod.BankTransfer && fallbackBankAccountId == bankAccountId.Value));
        foreach (var e in expQuery.ToList())
            lines.Add(new CashBookLineDto { Date = e.IncurredDate, Description = $"Chi phí: {e.Title}", SourceType = "Expense", SourceId = e.Id, AmountOut = e.AmountExcludingTax });

        return lines;
    }

    private Guid? GetFallbackBankAccountId()
        => _bankAccounts.DataSource
            .Where(a => a.IsActive)
            .OrderByDescending(a => a.IsDefault)
            .ThenBy(a => a.DisplayName)
            .Select(a => (Guid?)a.Id)
            .FirstOrDefault();

}
