using NamEcommerce.Data.Contracts;
using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Domain.Entities.Finance;
using NamEcommerce.Domain.Shared.Dtos.Finance;
using NamEcommerce.Domain.Shared.Exceptions.Finance;
using NamEcommerce.Domain.Shared.Services.Finance;
using Microsoft.EntityFrameworkCore;

namespace NamEcommerce.Domain.Services.Finance;

public sealed class AccountingSetupManager : IAccountingSetupManager
{
    private readonly IRepository<AccountingSetup> _repository;
    private readonly IEntityDataReader<AccountingSetup> _dataReader;

    public AccountingSetupManager(
        IRepository<AccountingSetup> repository,
        IEntityDataReader<AccountingSetup> dataReader)
    {
        _repository = repository;
        _dataReader = dataReader;
    }

    public async Task<AccountingSetupDto?> GetAsync()
    {
        var setup = await _dataReader.DataSource.FirstOrDefaultAsync().ConfigureAwait(false);
        return setup?.ToDto();
    }

    public async Task<AccountingSetupDto> SaveAsync(SaveAccountingSetupDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        dto.Verify();

        var existingId = await _dataReader.DataSource.Select(x => x.Id).FirstOrDefaultAsync().ConfigureAwait(false);
        if (existingId == Guid.Empty)
        {
            var setup = new AccountingSetup(
                dto.FiscalYearStartMonth, dto.FiscalYearStartDay,
                dto.AccountingStartDate, dto.OpeningCash,
                dto.OpeningEquity, dto.DefaultTaxRate);
            var inserted = await _repository.InsertAsync(setup).ConfigureAwait(false);
            return inserted.ToDto();
        }

        var existing = await _repository.GetByIdAsync(existingId).ConfigureAwait(false)
            ?? throw new AccountingSetupNotFoundException();
        existing.Update(dto.FiscalYearStartMonth, dto.FiscalYearStartDay,
            dto.AccountingStartDate, dto.OpeningCash, dto.OpeningEquity, dto.DefaultTaxRate);
        var updated = await _repository.UpdateAsync(existing).ConfigureAwait(false);
        return updated.ToDto();
    }

    public async Task FinalizeAsync()
    {
        var setupId = await _dataReader.DataSource.Select(x => x.Id).FirstOrDefaultAsync().ConfigureAwait(false);
        if (setupId == Guid.Empty) throw new AccountingSetupNotFoundException();
        var setup = await _repository.GetByIdAsync(setupId).ConfigureAwait(false)
            ?? throw new AccountingSetupNotFoundException();
        setup.ConfirmAndLock();
        await _repository.UpdateAsync(setup).ConfigureAwait(false);
    }

    public async Task UpdateCorporateTaxProvisionAsync(decimal? amount)
    {
        var setupId = await _dataReader.DataSource.Select(x => x.Id).FirstOrDefaultAsync().ConfigureAwait(false);
        if (setupId == Guid.Empty) throw new AccountingSetupNotFoundException();
        var setup = await _repository.GetByIdAsync(setupId).ConfigureAwait(false)
            ?? throw new AccountingSetupNotFoundException();
        setup.UpdateCorporateTaxProvision(amount);
        await _repository.UpdateAsync(setup).ConfigureAwait(false);
    }
}

internal static class AccountingSetupExtensions
{
    internal static AccountingSetupDto ToDto(this AccountingSetup s) => new()
    {
        Id = s.Id,
        FiscalYearStartMonth = s.FiscalYearStartMonth,
        FiscalYearStartDay = s.FiscalYearStartDay,
        AccountingStartDate = s.AccountingStartDate,
        OpeningCash = s.OpeningCash,
        OpeningEquity = s.OpeningEquity,
        DefaultTaxRate = s.DefaultTaxRate,
        CorporateTaxProvision = s.CorporateTaxProvision,
        IsFinalized = s.IsFinalized,
        FinalizedOnUtc = s.FinalizedOnUtc
    };
}
