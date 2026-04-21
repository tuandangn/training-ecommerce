using MediatR;
using NamEcommerce.Application.Contracts.Debts;
using NamEcommerce.Application.Contracts.Dtos.Debts;
using NamEcommerce.Web.Contracts.Commands.Models.Debts;
using NamEcommerce.Web.Contracts.Models.Common;
using NamEcommerce.Web.Contracts.Services;

namespace NamEcommerce.Web.Framework.Commands.Handlers.Debts;

public sealed class RecordFlexibleVendorPaymentHandler(
    IVendorDebtAppService debtAppService,
    ICurrentUserService currentUserService) : IRequestHandler<RecordFlexibleVendorPaymentCommand, CommonActionResultModel>
{
    private readonly IVendorDebtAppService _debtAppService = debtAppService;
    private readonly ICurrentUserService _currentUserService = currentUserService;

    public async Task<CommonActionResultModel> Handle(RecordFlexibleVendorPaymentCommand request, CancellationToken cancellationToken)
    {
        var currentUser = await _currentUserService.GetCurrentUserInfoAsync().ConfigureAwait(false);

        var dto = new CreateVendorPaymentAppDto
        {
            VendorId = request.Model.VendorId,
            // VendorDebtId không set → hệ thống tự phân bổ FIFO
            Amount = request.Model.Amount,
            PaymentMethod = request.Model.PaymentMethod,
            PaymentType = request.Model.PaymentType,
            Note = request.Model.Note,
            PaidOnUtc = request.Model.PaidOn.ToUniversalTime(),
            RecordedByUserId = currentUser?.Id
        };

        var result = await _debtAppService.RecordFlexiblePaymentForVendorAsync(dto).ConfigureAwait(false);
        if (!result.Success)
            return new CommonActionResultModel { Success = false, ErrorMessage = result.ErrorMessage };

        return new CommonActionResultModel
        {
            Success = true,
            SuccessMessage = $"Đã ghi nhận {result.Payments.Count} giao dịch chi tiền cho nhà cung cấp."
        };
    }
}
