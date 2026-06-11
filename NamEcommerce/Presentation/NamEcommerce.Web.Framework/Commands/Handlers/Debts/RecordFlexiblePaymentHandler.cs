using MediatR;
using NamEcommerce.Application.Contracts.Debts;
using NamEcommerce.Application.Contracts.Dtos.Debts;
using NamEcommerce.Application.Contracts.Users;
using NamEcommerce.Web.Contracts.Commands.Models.Debts;
using NamEcommerce.Web.Contracts.Models.Common;

namespace NamEcommerce.Web.Framework.Commands.Handlers.Debts;

public sealed class RecordFlexiblePaymentHandler(
    ICustomerDebtAppService debtAppService,
    ICurrentUserService currentUserService) : IRequestHandler<RecordFlexiblePaymentCommand, CommonActionResultModel>
{
    private readonly ICustomerDebtAppService _debtAppService = debtAppService;
    private readonly ICurrentUserService _currentUserService = currentUserService;

    public async Task<CommonActionResultModel> Handle(RecordFlexiblePaymentCommand request, CancellationToken cancellationToken)
    {
        var currentUser = await _currentUserService.GetCurrentUserInfoAsync().ConfigureAwait(false);
        var dto = new CreateCustomerPaymentAppDto
        {
            CustomerId = request.Model.CustomerId,
            Amount = request.Model.Amount,
            PaymentMethod = request.Model.PaymentMethod,
            Note = request.Model.Note,
            PaidOnUtc = request.Model.PaidOnUtc.ToUniversalTime(),
            RecordedByUserId = currentUser?.Id
        };

        var payments = await _debtAppService.RecordFlexiblePaymentForCustomerAsync(dto).ConfigureAwait(false);
        return new CommonActionResultModel
        {
            Success = true,
            SuccessMessage = $"Đã ghi nhận {payments.Count} giao dịch thanh toán."
        };
    }
}
