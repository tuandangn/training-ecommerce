using MediatR;
using NamEcommerce.Application.Contracts.Customers;
using NamEcommerce.Application.Contracts.Debts;
using NamEcommerce.Domain.Shared.Settings;
using NamEcommerce.Web.Contracts.Queries.Models.Inventory;
using NamEcommerce.Web.Models.FastSales;

namespace NamEcommerce.Web.Services.FastSales;

public sealed class FastSaleModelFactory(
    IMediator mediator,
    ICustomerAppService customerAppService,
    IBankTransferReceivingAccountResolver receivingAccountResolver,
    BankTransferPaymentSettings bankTransferPaymentSettings) : IFastSaleModelFactory
{
    public async Task<FastSaleModel> PrepareFastSaleModelAsync()
    {
        var retailWalkInCustomer = await customerAppService.GetOrCreateRetailWalkInCustomerAsync().ConfigureAwait(false);

        var warehouses = await mediator.Send(new GetWarehouseOptionListQuery
        {
            IncludeDirectTransit = false
        }).ConfigureAwait(false);

        var receivingAccount = await receivingAccountResolver.ResolveAsync().ConfigureAwait(false);

        return new FastSaleModel
        {
            DefaultCustomerId = retailWalkInCustomer.Id,
            DefaultCustomerName = retailWalkInCustomer.FullName,
            DefaultCustomerPhone = retailWalkInCustomer.PhoneNumber,
            DefaultCustomerAddress = retailWalkInCustomer.Address,
            Warehouses = warehouses.Options,
            BankTransferEnabled = bankTransferPaymentSettings.Enabled && receivingAccount?.IsConfigured == true,
            BankAccountLabel = string.IsNullOrWhiteSpace(receivingAccount?.AccountNo)
                ? string.Empty
                : $"{receivingAccount.AccountName} - {receivingAccount.AccountNo}",
            ManualBankTransferConfirmEnabled = bankTransferPaymentSettings.Verification.AllowManualConfirm
        };
    }
}
