using MediatR;
using NamEcommerce.Application.Contracts.PurchaseOrders;
using NamEcommerce.Application.Contracts.Security;
using NamEcommerce.Application.Contracts.Users;
using NamEcommerce.Web.Contracts.Commands.Models.PurchaseOrders;
using NamEcommerce.Web.Contracts.Models.Common;
using NamEcommerce.Web.Contracts.Security;

namespace NamEcommerce.Web.Framework.Commands.Handlers.PurchaseOrders;

public sealed class ApprovesPurchaseOrderHandler : IRequestHandler<ApprovesPurchaseOrderCommand, CommonActionResultModel>
{
    private readonly IPurchaseOrderAppService _purchaseOrderAppService;
    private readonly IAuthorizationAppService _authorizationAppService;
    private readonly ICurrentUserService _currentUserService;

    public ApprovesPurchaseOrderHandler(IPurchaseOrderAppService purchaseOrderAppService,
        IAuthorizationAppService authorizationAppService, ICurrentUserService currentUserService)
    {
        _purchaseOrderAppService = purchaseOrderAppService;
        _authorizationAppService = authorizationAppService;
        _currentUserService = currentUserService;
    }

    public async Task<CommonActionResultModel> Handle(ApprovesPurchaseOrderCommand request, CancellationToken cancellationToken)
    {
        var currentUser = await _currentUserService.GetCurrentUserInfoAsync().ConfigureAwait(false);
        if (currentUser is null)
        {
            return new CommonActionResultModel
            {
                Success = false,
                ErrorMessage = "Error.UserNotAuthorized"
            };
        }

        if (!await _currentUserService.IsAdminAsync() && !await _authorizationAppService.Authorize(currentUser.Id, SystemPermissions.PurchaseOrders.Approve).ConfigureAwait(false))
        {
            return new CommonActionResultModel
            {
                Success = false,
                ErrorMessage = "Error.UserNotAuthorized"
            };
        }

        var (success, errorMessage) = await _purchaseOrderAppService.ApprovePurchaseOrderAsync(request.PurchaseOrderId).ConfigureAwait(false);

        return new CommonActionResultModel
        {
            Success = success,
            ErrorMessage = errorMessage
        };
    }
}
