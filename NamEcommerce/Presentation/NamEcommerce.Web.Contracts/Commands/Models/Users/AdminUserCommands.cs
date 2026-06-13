using NamEcommerce.Web.Contracts.Models.Common;

namespace NamEcommerce.Web.Contracts.Commands.Models.Users;

[Serializable]
public sealed record CreateAdminUserCommand(
    string Username,
    string Password,
    string FullName,
    string PhoneNumber,
    string? Address) : ICommand<CommonActionResultModel>;

[Serializable]
public sealed record LockUserCommand(Guid UserId) : ICommand<CommonActionResultModel>;

[Serializable]
public sealed record UnlockUserCommand(Guid UserId) : ICommand<CommonActionResultModel>;

[Serializable]
public sealed record DeleteUserCommand(Guid UserId) : ICommand<CommonActionResultModel>;

[Serializable]
public sealed record ChangeUserPasswordCommand(Guid UserId, string NewPassword) : ICommand<CommonActionResultModel>;
