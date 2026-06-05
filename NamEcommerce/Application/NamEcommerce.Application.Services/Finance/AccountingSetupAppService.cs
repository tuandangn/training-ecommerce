using NamEcommerce.Application.Contracts.Dtos.Finance;
using NamEcommerce.Application.Contracts.Finance;
using NamEcommerce.Domain.Shared.Dtos.Finance;
using NamEcommerce.Domain.Shared.Exceptions.Finance;
using NamEcommerce.Domain.Shared.Services.Finance;

namespace NamEcommerce.Application.Services.Finance;

public sealed class AccountingSetupAppService : IAccountingSetupAppService
{
    private readonly IAccountingSetupManager _manager;

    public AccountingSetupAppService(IAccountingSetupManager manager)
        => _manager = manager;

    public async Task<AccountingSetupAppDto> GetSetupAsync()
    {
        var dto = await _manager.GetAsync().ConfigureAwait(false);
        if (dto is null)
            return new AccountingSetupAppDto { IsConfigured = false };

        return new AccountingSetupAppDto
        {
            Id = dto.Id,
            IsConfigured = true,
            IsFinalized = dto.IsFinalized,
            FiscalYearStartMonth = dto.FiscalYearStartMonth,
            FiscalYearStartDay = dto.FiscalYearStartDay,
            AccountingStartDate = dto.AccountingStartDate,
            OpeningCash = dto.OpeningCash,
            OpeningEquity = dto.OpeningEquity,
            DefaultTaxRate = dto.DefaultTaxRate,
            CorporateTaxProvision = dto.CorporateTaxProvision,
            FinalizedOnUtc = dto.FinalizedOnUtc
        };
    }

    public async Task<AccountingSetupOperationResultAppDto> SaveSetupAsync(SaveAccountingSetupAppDto dto)
    {
        var (valid, error) = dto.Validate();
        if (!valid) return Fail(error);

        try
        {
            await _manager.SaveAsync(new SaveAccountingSetupDto
            {
                FiscalYearStartMonth = dto.FiscalYearStartMonth,
                FiscalYearStartDay = dto.FiscalYearStartDay,
                AccountingStartDate = dto.AccountingStartDate,
                OpeningCash = dto.OpeningCash,
                OpeningEquity = dto.OpeningEquity,
                DefaultTaxRate = dto.DefaultTaxRate
            }).ConfigureAwait(false);
            return Ok();
        }
        catch (AccountingSetupAlreadyFinalizedException ex) { return Fail(ex.Message); }
    }

    public async Task<AccountingSetupOperationResultAppDto> FinalizeSetupAsync()
    {
        try
        {
            await _manager.FinalizeAsync().ConfigureAwait(false);
            return Ok();
        }
        catch (AccountingSetupNotFoundException ex) { return Fail(ex.Message); }
        catch (AccountingSetupAlreadyFinalizedException ex) { return Fail(ex.Message); }
    }

    public async Task<AccountingSetupOperationResultAppDto> UpdateCorporateTaxProvisionAsync(decimal? amount)
    {
        if (amount.HasValue && amount.Value < 0)
            return Fail("Error.Accounting.CorporateTaxProvisionCannotBeNegative");
        try
        {
            await _manager.UpdateCorporateTaxProvisionAsync(amount).ConfigureAwait(false);
            return Ok();
        }
        catch (AccountingSetupNotFoundException ex) { return Fail(ex.Message); }
    }

    private static AccountingSetupOperationResultAppDto Ok() => new() { Success = true };
    private static AccountingSetupOperationResultAppDto Fail(string? msg) => new() { Success = false, ErrorMessage = msg };
}
