using NamEcommerce.Web.Contracts.Models.Common;

namespace NamEcommerce.Web.Contracts.Commands.Models.Users;

[Serializable]
public sealed record UpdateUserRolesCommand(Guid UserId, IList<Guid> RoleIds) : ICommand<CommonActionResultModel>;
