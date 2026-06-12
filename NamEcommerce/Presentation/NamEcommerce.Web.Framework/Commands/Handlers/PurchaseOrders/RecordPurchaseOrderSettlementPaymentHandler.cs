using MediatR;
using NamEcommerce.Application.Contracts.Debts;
using NamEcommerce.Application.Contracts.Dtos.Debts;
using NamEcommerce.Application.Contracts.Finance;
using NamEcommerce.Application.Contracts.PurchaseOrders;
using NamEcommerce.Application.Contracts.Users;
using NamEcommerce.Domain.Shared.Enums.Debts;
using NamEcommerce.Domain.Shared.Enums.Orders;
using NamEcommerce.Web.Contracts.Commands.Models.PurchaseOrders;
using NamEcommerce.Web.Contracts.Models.Common;

namespace NamEcommerce.Web.Framework.Commands.Handlers.PurchaseOrders;

public sealed class RecordPurchaseOrderSettlementPaymentHandler(
    IPurchaseOrderAppService purchaseOrderAppService,
    IVendorDebtAppService vendorDebtAppService,
    IBankAccountAppService bankAccountAppService,
    ICurrentUserService currentUserService) : IRequestHandler<RecordPurchaseOrderSettlementPaymentCommand, CommonActionResultModel>
{
    public async Task<CommonActionResultModel> Handle(RecordPurchaseOrderSettlementPaymentCommand request, CancellationToken cancellationToken)
    {
        if (request.Amount <= 0)
            return new CommonActionResultModel { Success = false, ErrorMessage = "Error.PaymentAmountMustBePositive" };

        var paymentMethod = (PaymentMethod)request.PaymentMethod;
        if (paymentMethod is not PaymentMethod.Cash and not PaymentMethod.BankTransfer)
            return new CommonActionResultModel { Success = false, ErrorMessage = "Error.InvalidRequest" };

        var purchaseOrder = await purchaseOrderAppService.GetPurchaseOrderByIdAsync(request.PurchaseOrderId).ConfigureAwait(false);
        if (purchaseOrder is null)
            return new CommonActionResultModel { Success = false, ErrorMessage = "Error.PurchaseOrderIsNotFound" };

        var bankAccountId = await ResolveBankAccountIdAsync(paymentMethod).ConfigureAwait(false);
        if (paymentMethod == PaymentMethod.BankTransfer && bankAccountId is null)
            return new CommonActionResultModel { Success = false, ErrorMessage = "Error.BankTransferAccountNotConfigured" };

        var currentUser = await currentUserService.GetCurrentUserInfoAsync().ConfigureAwait(false);
        var result = await vendorDebtAppService.RecordPaymentAsync(new CreateVendorPaymentAppDto
        {
            VendorId = purchaseOrder.VendorId,
            PurchaseOrderId = purchaseOrder.Id,
            Amount = request.Amount,
            PaymentMethod = request.PaymentMethod,
            PaymentType = (int)PaymentType.VendorDebtPayment,
            BankAccountId = bankAccountId,
            Note = request.Note,
            PaidOnUtc = request.PaidOn.ToUniversalTime(),
            RecordedByUserId = currentUser?.Id
        }).ConfigureAwait(false);

        return new CommonActionResultModel
        {
            Success = result.Success,
            ErrorMessage = result.ErrorMessage
        };
    }

    private async Task<Guid?> ResolveBankAccountIdAsync(PaymentMethod paymentMethod)
    {
        if (paymentMethod != PaymentMethod.BankTransfer)
            return null;

        var accounts = await bankAccountAppService.GetBankAccountsAsync().ConfigureAwait(false);
        return accounts.FirstOrDefault(a => a.IsDefault)?.Id ?? accounts.FirstOrDefault()?.Id;
    }
}
