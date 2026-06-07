using NamEcommerce.Application.Contracts.Dtos.Finance;

namespace NamEcommerce.Application.Contracts.Finance;

public interface IAccountingSetupAppService
{
    Task<AccountingSetupAppDto> GetSetupAsync();
    Task<AccountingSetupOperationResultAppDto> SaveSetupAsync(SaveAccountingSetupAppDto dto);
    Task<AccountingSetupOperationResultAppDto> FinalizeSetupAsync();
    Task<AccountingSetupOperationResultAppDto> UpdateCorporateTaxProvisionAsync(decimal? amount);
}
