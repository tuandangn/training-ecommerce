using MediatR;
using NamEcommerce.Application.Contracts.Debts;
using NamEcommerce.Application.Contracts.Dtos.Debts;
using NamEcommerce.Web.Contracts.Commands.Models.Debts;
using NamEcommerce.Web.Contracts.Models.Common;
using NamEcommerce.Web.Contracts.Services;

namespace NamEcommerce.Web.Framework.Commands.Handlers.Debts;

public sealed class RecordVendorPaymentHandler(
    IVendorDebtAppService debtAppService,
    ICurrentUserService currentUserService) : IRequestHandler<RecordVendorPaymentCommand, CommonActionResultModel>
{
    private readonly IVendorDebtAppService _debtAppService = debtAppService;
    private readonly ICurrentUserService _currentUserService = currentUserService;

    public async Task<CommonActionResultModel> Handle(RecordVendorPaymentCommand request, CancellationToken cancellationToken)
    {
        var currentUser = await _currentUserService.GetCurrentUserInfoAsync().ConfigureAwait(false);

        var dto = new CreateVendorPaymentAppDto
        {
            VendorId = request.Model.VendorId,
            VendorDebtId = request.Model.VendorDebtId,
            PurchaseOrderId = request.Model.PurchaseOrderId,
            Amount = request.Model.Amount,
            PaymentMethod = request.Model.PaymentMethod,
            PaymentType = request.Model.PaymentType,
            Note = request.Model.Note,
            PaidOnUtc = request.Model.PaidOn.ToUniversalTime(),
            RecordedByUserId = currentUser?.Id
        };

        var result = await _debtAppService.RecordPaymentAsync(dto).ConfigureAwait(false);
        return new CommonActionResultModel { Success = result.Success, ErrorMessage = result.ErrorMessage };
    }
}
