using MediatR;
using NamEcommerce.Application.Contracts.Debts;
using NamEcommerce.Application.Contracts.Dtos.Debts;
using NamEcommerce.Web.Contracts.Commands.Models.Debts;
using NamEcommerce.Web.Contracts.Models.Common;
using NamEcommerce.Web.Contracts.Services;

namespace NamEcommerce.Web.Framework.Commands.Handlers.Debts;

public sealed class RecordFlexiblePaymentHandler(
    ICustomerDebtAppService debtAppService,
    ICurrentUserService currentUserService) : IRequestHandler<RecordFlexiblePaymentCommand, CommonResultModel>
{
    private readonly ICustomerDebtAppService _debtAppService = debtAppService;
    private readonly ICurrentUserService _currentUserService = currentUserService;

    public async Task<CommonResultModel> Handle(RecordFlexiblePaymentCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var currentUser = await _currentUserService.GetCurrentUserInfoAsync().ConfigureAwait(false);

            var dto = new CreateCustomerPaymentAppDto
            {
                CustomerId = request.Model.CustomerId,
                // CustomerDebtId không set → hệ thống tự phân bổ FIFO
                Amount = request.Model.Amount,
                PaymentMethod = request.Model.PaymentMethod,
                PaymentType = request.Model.PaymentType,
                Note = request.Model.Note,
                PaidOnUtc = request.Model.PaidOnUtc.ToUniversalTime(),
                RecordedByUserId = currentUser?.Id
            };

            var payments = await _debtAppService.RecordFlexiblePaymentForCustomerAsync(dto).ConfigureAwait(false);
            return new CommonResultModel
            {
                Success = true,
                SuccessMessage = $"Đã ghi nhận {payments.Count} giao dịch thanh toán."
            };
        }
        catch (Exception ex)
        {
            return new CommonResultModel { Success = false, ErrorMessage = ex.Message };
        }
    }
}
