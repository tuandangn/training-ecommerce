using NamEcommerce.Domain.Shared.Dtos.Finance;

namespace NamEcommerce.Domain.Shared.Services.Finance;

public interface IAccountingSetupManager
{
    Task<AccountingSetupDto?> GetAsync();
    Task<AccountingSetupDto> SaveAsync(SaveAccountingSetupDto dto);
    Task FinalizeAsync();
    Task UpdateCorporateTaxProvisionAsync(decimal? amount);
}
