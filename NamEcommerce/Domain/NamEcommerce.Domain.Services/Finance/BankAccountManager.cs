using NamEcommerce.Data.Contracts;
using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Domain.Entities.Finance;
using NamEcommerce.Domain.Shared.Dtos.Finance;
using NamEcommerce.Domain.Shared.Exceptions.Finance;
using NamEcommerce.Domain.Shared.Services.Finance;

namespace NamEcommerce.Domain.Services.Finance;

public sealed class BankAccountManager : IBankAccountManager
{
    private readonly IRepository<BankAccount> _repository;
    private readonly IEntityDataReader<BankAccount> _dataReader;

    public BankAccountManager(
        IRepository<BankAccount> repository,
        IEntityDataReader<BankAccount> dataReader)
    {
        _repository = repository;
        _dataReader = dataReader;
    }

    public Task<BankAccountDto?> GetByIdAsync(Guid id)
    {
        var account = _dataReader.DataSource.FirstOrDefault(a => a.Id == id);
        return Task.FromResult(account?.ToDto());
    }

    public Task<IReadOnlyList<BankAccountDto>> GetAllAsync(bool includeInactive = false)
    {
        var query = _dataReader.DataSource.AsEnumerable();
        if (!includeInactive) query = query.Where(a => a.IsActive);
        IReadOnlyList<BankAccountDto> result = query
            .OrderByDescending(a => a.IsDefault)
            .ThenBy(a => a.DisplayName)
            .Select(a => a.ToDto())
            .ToList();
        return Task.FromResult(result);
    }

    public Task<BankAccountDto?> GetDefaultAsync()
    {
        var account = _dataReader.DataSource.FirstOrDefault(a => a.IsDefault && a.IsActive);
        return Task.FromResult(account?.ToDto());
    }

    public async Task<CreateBankAccountResultDto> CreateAsync(CreateBankAccountDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        dto.Verify();

        var count = _dataReader.DataSource.Count();
        var code = $"NH-{(count + 1):D3}";

        var account = new BankAccount(code, dto.DisplayName, dto.BankName,
            dto.AccountNumber, dto.AccountHolderName, dto.OpeningBalance);

        var hasAny = _dataReader.DataSource.Any();
        if (dto.SetAsDefault || !hasAny)
        {
            await ClearAllDefaultsAsync().ConfigureAwait(false);
            account.SetAsDefault();
        }

        var inserted = await _repository.InsertAsync(account).ConfigureAwait(false);
        return new CreateBankAccountResultDto { CreatedId = inserted.Id };
    }

    public async Task UpdateAsync(UpdateBankAccountDto dto)
    {
        var account = _dataReader.DataSource.FirstOrDefault(a => a.Id == dto.Id)
            ?? throw new BankAccountNotFoundException(dto.Id);
        account.UpdateInfo(dto.DisplayName, dto.BankName, dto.AccountNumber, dto.AccountHolderName);
        await _repository.UpdateAsync(account).ConfigureAwait(false);
    }

    public async Task SetDefaultAsync(Guid id)
    {
        var account = _dataReader.DataSource.FirstOrDefault(a => a.Id == id)
            ?? throw new BankAccountNotFoundException(id);

        await ClearAllDefaultsAsync().ConfigureAwait(false);
        account.SetAsDefault();
        await _repository.UpdateAsync(account).ConfigureAwait(false);
    }

    public async Task DeactivateAsync(Guid id)
    {
        var account = _dataReader.DataSource.FirstOrDefault(a => a.Id == id)
            ?? throw new BankAccountNotFoundException(id);
        account.Deactivate();
        await _repository.UpdateAsync(account).ConfigureAwait(false);
    }

    public async Task ActivateAsync(Guid id)
    {
        var account = _dataReader.DataSource.FirstOrDefault(a => a.Id == id)
            ?? throw new BankAccountNotFoundException(id);
        account.Activate();
        await _repository.UpdateAsync(account).ConfigureAwait(false);
    }

    private async Task ClearAllDefaultsAsync()
    {
        var currentDefaults = _dataReader.DataSource.Where(a => a.IsDefault).ToList();
        foreach (var a in currentDefaults)
        {
            a.ClearDefault();
            await _repository.UpdateAsync(a).ConfigureAwait(false);
        }
    }
}

internal static class BankAccountExtensions
{
    internal static BankAccountDto ToDto(this BankAccount a) => new()
    {
        Id = a.Id,
        Code = a.Code,
        DisplayName = a.DisplayName,
        BankName = a.BankName,
        AccountNumber = a.AccountNumber,
        AccountHolderName = a.AccountHolderName,
        OpeningBalance = a.OpeningBalance,
        IsDefault = a.IsDefault,
        IsActive = a.IsActive
    };
}
