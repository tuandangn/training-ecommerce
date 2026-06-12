using MediatR;
using NamEcommerce.Application.Contracts.Debts;
using NamEcommerce.Application.Contracts.Dtos.Debts;
using NamEcommerce.Application.Contracts.Orders;
using NamEcommerce.Application.Contracts.Users;
using NamEcommerce.Domain.Shared.Enums.Debts;
using NamEcommerce.Domain.Shared.Enums.Orders;
using NamEcommerce.Web.Contracts.Commands.Models.Orders;
using NamEcommerce.Web.Contracts.Models.Common;

namespace NamEcommerce.Web.Framework.Commands.Handlers.Orders;

public sealed class RecordOrderSettlementPaymentHandler(
    IOrderAppService orderAppService,
    ICustomerDebtAppService customerDebtAppService,
    ICurrentUserService currentUserService) : IRequestHandler<RecordOrderSettlementPaymentCommand, CommonActionResultModel>
{
    public async Task<CommonActionResultModel> Handle(RecordOrderSettlementPaymentCommand request, CancellationToken cancellationToken)
    {
        if (request.Amount <= 0)
            return new CommonActionResultModel { Success = false, ErrorMessage = "Error.PaymentAmountMustBePositive" };

        if ((PaymentMethod)request.PaymentMethod != PaymentMethod.Cash)
            return new CommonActionResultModel { Success = false, ErrorMessage = "Error.InvalidRequest" };

        var order = await orderAppService.GetOrderByIdAsync(request.OrderId).ConfigureAwait(false);
        if (order is null)
            return new CommonActionResultModel { Success = false, ErrorMessage = "Error.OrderIsNotFound" };

        var currentUser = await currentUserService.GetCurrentUserInfoAsync().ConfigureAwait(false);
        await customerDebtAppService.RecordPaymentAsync(new CreateCustomerPaymentAppDto
        {
            CustomerId = order.CustomerId,
            OrderId = order.Id,
            Amount = request.Amount,
            PaymentMethod = request.PaymentMethod,
            PaymentType = (int)PaymentType.DebtPayment,
            Note = request.Note,
            PaidOnUtc = request.PaidOn.ToUniversalTime(),
            RecordedByUserId = currentUser?.Id
        }).ConfigureAwait(false);

        return new CommonActionResultModel { Success = true };
    }
}

public sealed class RecordOrderSettlementQrPaymentHandler(
    IOrderAppService orderAppService,
    ICustomerDebtAppService customerDebtAppService,
    IBankTransferPaymentIntentAppService paymentIntentAppService,
    IBankTransferReceivingAccountResolver receivingAccountResolver,
    ICurrentUserService currentUserService) : IRequestHandler<RecordOrderSettlementQrPaymentCommand, CommonActionResultModel>
{
    public async Task<CommonActionResultModel> Handle(RecordOrderSettlementQrPaymentCommand request, CancellationToken cancellationToken)
    {
        var order = await orderAppService.GetOrderByIdAsync(request.OrderId).ConfigureAwait(false);
        if (order is null)
            return new CommonActionResultModel { Success = false, ErrorMessage = "Error.OrderIsNotFound" };

        var status = await paymentIntentAppService.GetStatusAsync(request.PaymentIntentId).ConfigureAwait(false);
        if (!status.Success || status.Intent is null)
            return new CommonActionResultModel { Success = false, ErrorMessage = status.ErrorMessage ?? "Error.PaymentIntentIsNotFound" };

        var intent = status.Intent;
        if (intent.CustomerId.HasValue && intent.CustomerId.Value != order.CustomerId)
            return new CommonActionResultModel { Success = false, ErrorMessage = "Error.InvalidRequest" };

        var intentStatus = (BankTransferPaymentIntentStatus)intent.Status;
        if (intentStatus is not BankTransferPaymentIntentStatus.Confirmed and not BankTransferPaymentIntentStatus.ManuallyConfirmed)
            return new CommonActionResultModel { Success = false, ErrorMessage = "Error.PaymentIntentIsNotConfirmed" };

        var receivingAccount = await receivingAccountResolver.ResolveAsync().ConfigureAwait(false);
        if (receivingAccount?.BankAccountId is null)
            return new CommonActionResultModel { Success = false, ErrorMessage = "Error.BankTransferAccountNotConfigured" };

        var currentUser = await currentUserService.GetCurrentUserInfoAsync().ConfigureAwait(false);
        var payment = await customerDebtAppService.RecordPaymentAsync(new CreateCustomerPaymentAppDto
        {
            CustomerId = order.CustomerId,
            OrderId = order.Id,
            Amount = intent.Amount,
            PaymentMethod = (int)PaymentMethod.BankTransfer,
            PaymentType = (int)PaymentType.DebtPayment,
            BankAccountId = receivingAccount.BankAccountId,
            Note = string.IsNullOrWhiteSpace(request.Note) ? intent.Note : request.Note,
            PaidOnUtc = DateTime.UtcNow,
            RecordedByUserId = currentUser?.Id
        }).ConfigureAwait(false);

        var consumeResult = await paymentIntentAppService.ConsumeAsync(new ConsumeBankTransferPaymentIntentAppDto
        {
            IntentId = intent.Id,
            OrderId = order.Id,
            CustomerPaymentId = payment.Id
        }).ConfigureAwait(false);

        return new CommonActionResultModel
        {
            Success = consumeResult.Success,
            ErrorMessage = consumeResult.ErrorMessage
        };
    }
}
