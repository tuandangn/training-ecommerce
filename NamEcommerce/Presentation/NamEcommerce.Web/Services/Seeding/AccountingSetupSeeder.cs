using NamEcommerce.Application.Contracts.Dtos.Finance;
using NamEcommerce.Application.Contracts.Finance;

namespace NamEcommerce.Web.Services.Seeding;

public sealed class AccountingSetupSeeder(IAccountingSetupAppService accountingSetupAppService) : IDataSeeder
{
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var setup = await accountingSetupAppService.GetSetupAsync().ConfigureAwait(false);
        if (setup.IsConfigured)
            return;

        var today = DateTime.Today;
        await accountingSetupAppService.SaveSetupAsync(new SaveAccountingSetupAppDto
        {
            FiscalYearStartMonth = 1,
            FiscalYearStartDay = 1,
            AccountingStartDate = new DateTime(today.Year, 1, 1),
            OpeningCash = 0,
            OpeningEquity = 0,
            DefaultTaxRate = 0.10m
        }).ConfigureAwait(false);
    }
}
