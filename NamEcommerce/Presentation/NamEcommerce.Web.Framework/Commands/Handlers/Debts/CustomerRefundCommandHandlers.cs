using MediatR;
using NamEcommerce.Application.Contracts.Debts;
using NamEcommerce.Application.Contracts.Dtos.Debts;
using NamEcommerce.Application.Contracts.Users;
using NamEcommerce.Web.Contracts.Commands.Models.Debts;
using NamEcommerce.Web.Contracts.Models.Common;

namespace NamEcommerce.Web.Framework.Commands.Handlers.Debts;

public sealed class CompleteCustomerRefundHandler(
    ICustomerRefundAppService customerRefundAppService,
    ICurrentUserService currentUserService)
    : IRequestHandler<CompleteCustomerRefundCommand, CommonActionResultModel>
{
    public async Task<CommonActionResultModel> Handle(CompleteCustomerRefundCommand request, CancellationToken cancellationToken)
    {
        var currentUser = await currentUserService.GetCurrentUserInfoAsync().ConfigureAwait(false);
        var result = await customerRefundAppService.CompleteAsync(new CompleteCustomerRefundAppDto
        {
            RefundId = request.Id,
            PaymentMethod = request.PaymentMethod,
            Note = request.Note,
            CompletedByUserId = currentUser?.Id
        }).ConfigureAwait(false);

        return new CommonActionResultModel { Success = result.Success, ErrorMessage = result.ErrorMessage };
    }
}

public sealed class CancelCustomerRefundHandler(ICustomerRefundAppService customerRefundAppService)
    : IRequestHandler<CancelCustomerRefundCommand, CommonActionResultModel>
{
    public async Task<CommonActionResultModel> Handle(CancelCustomerRefundCommand request, CancellationToken cancellationToken)
    {
        var result = await customerRefundAppService.CancelAsync(request.Id).ConfigureAwait(false);
        return new CommonActionResultModel { Success = result.Success, ErrorMessage = result.ErrorMessage };
    }
}
