using MediatR;
using NamEcommerce.Application.Contracts.Dtos.Users;
using NamEcommerce.Application.Contracts.Users;
using NamEcommerce.Web.Contracts.Commands.Models.Users;
using NamEcommerce.Web.Contracts.Models.Common;

namespace NamEcommerce.Web.Framework.Commands.Handlers.Users;

public sealed class CreateAdminUserHandler(IUserAppService userAppService)
    : IRequestHandler<CreateAdminUserCommand, CommonActionResultModel>
{
    public async Task<CommonActionResultModel> Handle(CreateAdminUserCommand request, CancellationToken cancellationToken)
    {
        var result = await userAppService.CreateUserAsync(new CreateUserAppDto(
            request.Username,
            request.Password,
            request.FullName,
            request.PhoneNumber)
        {
            Address = request.Address
        }).ConfigureAwait(false);

        return new CommonActionResultModel
        {
            Success = result.Success,
            ErrorMessage = result.ErrorMessage
        };
    }
}

public sealed class LockUserHandler(IUserAppService userAppService)
    : IRequestHandler<LockUserCommand, CommonActionResultModel>
{
    public async Task<CommonActionResultModel> Handle(LockUserCommand request, CancellationToken cancellationToken)
    {
        var result = await userAppService.LockUserAsync(request.UserId).ConfigureAwait(false);

        return new CommonActionResultModel
        {
            Success = result.Success,
            ErrorMessage = result.ErrorMessage
        };
    }
}

public sealed class UnlockUserHandler(IUserAppService userAppService)
    : IRequestHandler<UnlockUserCommand, CommonActionResultModel>
{
    public async Task<CommonActionResultModel> Handle(UnlockUserCommand request, CancellationToken cancellationToken)
    {
        var result = await userAppService.UnlockUserAsync(request.UserId).ConfigureAwait(false);

        return new CommonActionResultModel
        {
            Success = result.Success,
            ErrorMessage = result.ErrorMessage
        };
    }
}

public sealed class DeleteUserHandler(IUserAppService userAppService)
    : IRequestHandler<DeleteUserCommand, CommonActionResultModel>
{
    public async Task<CommonActionResultModel> Handle(DeleteUserCommand request, CancellationToken cancellationToken)
    {
        var result = await userAppService.DeleteUserAsync(request.UserId).ConfigureAwait(false);

        return new CommonActionResultModel
        {
            Success = result.Success,
            ErrorMessage = result.ErrorMessage
        };
    }
}

public sealed class ChangeUserPasswordHandler(IUserAppService userAppService)
    : IRequestHandler<ChangeUserPasswordCommand, CommonActionResultModel>
{
    public async Task<CommonActionResultModel> Handle(ChangeUserPasswordCommand request, CancellationToken cancellationToken)
    {
        var result = await userAppService.ChangePasswordAsync(request.UserId, request.NewPassword).ConfigureAwait(false);

        return new CommonActionResultModel
        {
            Success = result.Success,
            ErrorMessage = result.ErrorMessage
        };
    }
}
