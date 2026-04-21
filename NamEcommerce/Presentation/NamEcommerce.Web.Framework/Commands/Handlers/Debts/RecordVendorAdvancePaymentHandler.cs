using MediatR;
using NamEcommerce.Application.Contracts.Debts;
using NamEcommerce.Application.Contracts.Dtos.Debts;
using NamEcommerce.Web.Contracts.Commands.Models.Debts;
using NamEcommerce.Web.Contracts.Models.Common;
using NamEcommerce.Web.Contracts.Services;

namespace NamEcommerce.Web.Framework.Commands.Handlers.Debts;

public sealed class RecordVendorAdvancePaymentHandler(
    IVendorDebtAppService debtAppService,
    ICurrentUserService currentUserService) : IRequestHandler<RecordVendorAdvancePaymentCommand, CommonActionResultModel>
{
    private readonly IVendorDebtAppService _debtAppService = debtAppService;
    private readonly ICurrentUserService _currentUserService = currentUserService;

    public async Task<CommonActionResultModel> Handle(RecordVendorAdvancePaymentCommand request, CancellationToken cancellationToken)
    {
        var currentUser = await _currentUserService.GetCurrentUserInfoAsync().ConfigureAwait(false);

        var dto = new CreateVendorPaymentAppDto
        {
            VendorId = request.Model.VendorId,
            Amount = request.Model.Amount,
            PaymentMethod = request.Model.PaymentMethod,
            PaymentType = request.Model.PaymentType,
            Note = request.Model.Note,
            PaidOnUtc = request.Model.PaidOn.ToUniversalTime(),
            RecordedByUserId = currentUser?.Id
        };

        var result = await _debtAppService.RecordAdvancePaymentAsync(dto).ConfigureAwait(false);
        return new CommonActionResultModel { Success = result.Success, ErrorMessage = result.ErrorMessage };
    }
}
