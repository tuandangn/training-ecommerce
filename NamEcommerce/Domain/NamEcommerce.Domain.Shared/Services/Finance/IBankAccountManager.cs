using NamEcommerce.Domain.Shared.Dtos.Finance;

namespace NamEcommerce.Domain.Shared.Services.Finance;

public interface IBankAccountManager
{
    Task<BankAccountDto?> GetByIdAsync(Guid id);
    Task<IReadOnlyList<BankAccountDto>> GetAllAsync(bool includeInactive = false);
    Task<BankAccountDto?> GetDefaultAsync();
    Task<CreateBankAccountResultDto> CreateAsync(CreateBankAccountDto dto);
    Task UpdateAsync(UpdateBankAccountDto dto);
    Task SetDefaultAsync(Guid id);
    Task DeactivateAsync(Guid id);
    Task ActivateAsync(Guid id);
}
