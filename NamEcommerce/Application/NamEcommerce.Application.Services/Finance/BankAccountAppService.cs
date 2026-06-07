using NamEcommerce.Application.Contracts.Dtos.Finance;
using NamEcommerce.Application.Contracts.Finance;
using NamEcommerce.Domain.Shared.Dtos.Finance;
using NamEcommerce.Domain.Shared.Exceptions.Finance;
using NamEcommerce.Domain.Shared.Services.Finance;

namespace NamEcommerce.Application.Services.Finance;

public sealed class BankAccountAppService : IBankAccountAppService
{
    private readonly IBankAccountManager _manager;

    public BankAccountAppService(IBankAccountManager manager)
        => _manager = manager;

    public async Task<IReadOnlyList<BankAccountAppDto>> GetBankAccountsAsync(bool includeInactive = false)
    {
        var dtos = await _manager.GetAllAsync(includeInactive).ConfigureAwait(false);
        return dtos.Select(ToAppDto).ToList();
    }

    public async Task<BankAccountAppDto?> GetBankAccountByIdAsync(Guid id)
    {
        var dto = await _manager.GetByIdAsync(id).ConfigureAwait(false);
        return dto is null ? null : ToAppDto(dto);
    }

    public async Task<BankAccountOperationResultAppDto> CreateBankAccountAsync(CreateBankAccountAppDto dto)
    {
        var (valid, error) = dto.Validate();
        if (!valid) return Fail(error);

        try
        {
            var result = await _manager.CreateAsync(new CreateBankAccountDto
            {
                DisplayName = dto.DisplayName,
                BankCode = dto.BankCode,
                BankName = dto.BankName,
                AccountNumber = dto.AccountNumber,
                AccountHolderName = dto.AccountHolderName,
                OpeningBalance = dto.OpeningBalance,
                SetAsDefault = dto.SetAsDefault
            }).ConfigureAwait(false);
            return new BankAccountOperationResultAppDto { Success = true, AccountId = result.CreatedId };
        }
        catch (BankAccountDataInvalidException ex) { return Fail(ex.Message); }
    }

    public async Task<BankAccountOperationResultAppDto> UpdateBankAccountAsync(UpdateBankAccountAppDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.DisplayName)) return Fail("Error.BankAccount.DisplayNameRequired");
        if (string.IsNullOrWhiteSpace(dto.BankCode)) return Fail("Error.BankAccount.BankCodeRequired");
        if (string.IsNullOrWhiteSpace(dto.BankName)) return Fail("Error.BankAccount.BankNameRequired");
        if (string.IsNullOrWhiteSpace(dto.AccountNumber)) return Fail("Error.BankAccount.AccountNumberRequired");
        if (string.IsNullOrWhiteSpace(dto.AccountHolderName)) return Fail("Error.BankAccount.AccountHolderNameRequired");
        try
        {
            await _manager.UpdateAsync(new UpdateBankAccountDto
            {
                Id = dto.Id,
                DisplayName = dto.DisplayName,
                BankCode = dto.BankCode,
                BankName = dto.BankName,
                AccountNumber = dto.AccountNumber,
                AccountHolderName = dto.AccountHolderName
            }).ConfigureAwait(false);
            return Ok();
        }
        catch (BankAccountNotFoundException ex) { return Fail(ex.Message); }
    }

    public async Task<BankAccountOperationResultAppDto> SetDefaultBankAccountAsync(Guid id)
    {
        try
        {
            await _manager.SetDefaultAsync(id).ConfigureAwait(false);
            return Ok();
        }
        catch (BankAccountNotFoundException ex) { return Fail(ex.Message); }
    }

    public async Task<BankAccountOperationResultAppDto> DeactivateBankAccountAsync(Guid id)
    {
        try
        {
            await _manager.DeactivateAsync(id).ConfigureAwait(false);
            return Ok();
        }
        catch (BankAccountNotFoundException ex) { return Fail(ex.Message); }
        catch (BankAccountIsDefaultException ex) { return Fail(ex.Message); }
    }

    public async Task<BankAccountOperationResultAppDto> ActivateBankAccountAsync(Guid id)
    {
        try
        {
            await _manager.ActivateAsync(id).ConfigureAwait(false);
            return Ok();
        }
        catch (BankAccountNotFoundException ex) { return Fail(ex.Message); }
    }

    private static BankAccountAppDto ToAppDto(BankAccountDto d) => new()
    {
        Id = d.Id,
        Code = d.Code,
        DisplayName = d.DisplayName,
        BankCode = d.BankCode,
        BankName = d.BankName,
        AccountNumber = d.AccountNumber,
        AccountHolderName = d.AccountHolderName,
        OpeningBalance = d.OpeningBalance,
        IsDefault = d.IsDefault,
        IsActive = d.IsActive
    };

    private static BankAccountOperationResultAppDto Ok() => new() { Success = true };
    private static BankAccountOperationResultAppDto Fail(string? msg) => new() { Success = false, ErrorMessage = msg };
}
