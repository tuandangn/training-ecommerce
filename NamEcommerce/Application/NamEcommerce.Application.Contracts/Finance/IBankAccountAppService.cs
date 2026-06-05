using NamEcommerce.Application.Contracts.Dtos.Finance;

namespace NamEcommerce.Application.Contracts.Finance;

public interface IBankAccountAppService
{
    Task<IReadOnlyList<BankAccountAppDto>> GetBankAccountsAsync(bool includeInactive = false);
    Task<BankAccountAppDto?> GetBankAccountByIdAsync(Guid id);
    Task<BankAccountOperationResultAppDto> CreateBankAccountAsync(CreateBankAccountAppDto dto);
    Task<BankAccountOperationResultAppDto> UpdateBankAccountAsync(UpdateBankAccountAppDto dto);
    Task<BankAccountOperationResultAppDto> SetDefaultBankAccountAsync(Guid id);
    Task<BankAccountOperationResultAppDto> DeactivateBankAccountAsync(Guid id);
    Task<BankAccountOperationResultAppDto> ActivateBankAccountAsync(Guid id);
}
