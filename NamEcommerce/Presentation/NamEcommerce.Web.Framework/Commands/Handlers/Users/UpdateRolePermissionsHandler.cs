using MediatR;
using NamEcommerce.Application.Contracts.Security;
using NamEcommerce.Web.Contracts.Commands.Models.Users;
using NamEcommerce.Web.Contracts.Models.Common;

namespace NamEcommerce.Web.Framework.Commands.Handlers.Users;

public sealed class UpdateRolePermissionsHandler(IPermissionAppService permissionAppService)
    : IRequestHandler<UpdateRolePermissionsCommand, CommonActionResultModel>
{
    public async Task<CommonActionResultModel> Handle(UpdateRolePermissionsCommand request, CancellationToken cancellationToken)
    {
        var (success, errorMessage) = await permissionAppService
            .UpdateRolePermissionsAsync(request.RoleId, request.PermissionIds, cancellationToken)
            .ConfigureAwait(false);

        return new CommonActionResultModel { Success = success, ErrorMessage = errorMessage };
    }
}
