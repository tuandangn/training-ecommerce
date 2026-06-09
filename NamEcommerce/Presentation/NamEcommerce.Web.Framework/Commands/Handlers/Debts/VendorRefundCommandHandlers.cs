using MediatR;
using NamEcommerce.Application.Contracts.Debts;
using NamEcommerce.Application.Contracts.Dtos.Debts;
using NamEcommerce.Application.Contracts.Users;
using NamEcommerce.Web.Contracts.Commands.Models.Debts;
using NamEcommerce.Web.Contracts.Models.Common;

namespace NamEcommerce.Web.Framework.Commands.Handlers.Debts;

public sealed class CompleteVendorRefundHandler(
    IVendorRefundAppService vendorRefundAppService,
    ICurrentUserService currentUserService)
    : IRequestHandler<CompleteVendorRefundCommand, CommonActionResultModel>
{
    public async Task<CommonActionResultModel> Handle(CompleteVendorRefundCommand request, CancellationToken cancellationToken)
    {
        var currentUser = await currentUserService.GetCurrentUserInfoAsync().ConfigureAwait(false);
        var result = await vendorRefundAppService.CompleteAsync(new CompleteVendorRefundAppDto
        {
            RefundId = request.Id,
            PaymentMethod = request.PaymentMethod,
            BankAccountId = request.BankAccountId,
            Note = request.Note,
            CompletedByUserId = currentUser?.Id
        }).ConfigureAwait(false);

        return new CommonActionResultModel { Success = result.Success, ErrorMessage = result.ErrorMessage };
    }
}

public sealed class CancelVendorRefundHandler(IVendorRefundAppService vendorRefundAppService)
    : IRequestHandler<CancelVendorRefundCommand, CommonActionResultModel>
{
    public async Task<CommonActionResultModel> Handle(CancelVendorRefundCommand request, CancellationToken cancellationToken)
    {
        var result = await vendorRefundAppService.CancelAsync(request.Id).ConfigureAwait(false);
        return new CommonActionResultModel { Success = result.Success, ErrorMessage = result.ErrorMessage };
    }
}
