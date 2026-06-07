using MediatR;
using NamEcommerce.Application.Contracts.Dtos.Users;
using NamEcommerce.Application.Contracts.Users;
using NamEcommerce.Web.Contracts.Commands.Models.Users;
using NamEcommerce.Web.Contracts.Models.Common;

namespace NamEcommerce.Web.Framework.Commands.Handlers.Users;

public sealed class UpdateUserRolesHandler(IUserAppService userAppService)
    : IRequestHandler<UpdateUserRolesCommand, CommonActionResultModel>
{
    public async Task<CommonActionResultModel> Handle(UpdateUserRolesCommand request, CancellationToken cancellationToken)
    {
        var result = await userAppService.UpdateUserRolesAsync(new UpdateUserRolesAppDto
        {
            UserId = request.UserId,
            RoleIds = request.RoleIds
        }).ConfigureAwait(false);

        return new CommonActionResultModel
        {
            Success = result.Success,
            ErrorMessage = result.ErrorMessage
        };
    }
}
