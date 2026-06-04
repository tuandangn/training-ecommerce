using MediatR;
using NamEcommerce.Domain.Shared.Settings;
using NamEcommerce.Web.Contracts.Models.Common;
using NamEcommerce.Web.Contracts.Queries.Models.Customers;
using NamEcommerce.Web.Contracts.Queries.Models.Inventory;
using NamEcommerce.Web.Models.FastSales;

namespace NamEcommerce.Web.Services.FastSales;

public sealed class FastSaleModelFactory(
    IMediator mediator,
    BankTransferPaymentSettings bankTransferPaymentSettings) : IFastSaleModelFactory
{
    public async Task<FastSaleModel> PrepareFastSaleModelAsync()
    {
        var customers = await mediator.Send(new GetCustomerListQuery
        {
            Keywords = null,
            PageIndex = 0,
            PageSize = int.MaxValue
        }).ConfigureAwait(false);

        var warehouses = await mediator.Send(new GetWarehouseOptionListQuery
        {
            IncludeDirectTransit = false
        }).ConfigureAwait(false);

        return new FastSaleModel
        {
            Customers = customers.Data.Items.Select(customer => new EntityOptionListModel.EntityOptionModel
            {
                Id = customer.Id,
                Name = string.IsNullOrWhiteSpace(customer.PhoneNumber)
                    ? customer.FullName
                    : $"{customer.FullName} - {customer.PhoneNumber}"
            }),
            Warehouses = warehouses.Options,
            BankTransferEnabled = bankTransferPaymentSettings.Enabled
                && !string.IsNullOrWhiteSpace(bankTransferPaymentSettings.BankId)
                && !string.IsNullOrWhiteSpace(bankTransferPaymentSettings.AccountNo)
                && !string.IsNullOrWhiteSpace(bankTransferPaymentSettings.AccountName),
            BankAccountLabel = string.IsNullOrWhiteSpace(bankTransferPaymentSettings.AccountNo)
                ? string.Empty
                : $"{bankTransferPaymentSettings.AccountName} - {bankTransferPaymentSettings.AccountNo}",
            ManualBankTransferConfirmEnabled = bankTransferPaymentSettings.Verification.AllowManualConfirm
        };
    }
}
