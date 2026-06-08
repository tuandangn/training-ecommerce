using MediatR;
using NamEcommerce.Web.Contracts.Models.Common;

namespace NamEcommerce.Web.Contracts.Commands.Models.Users;

[Serializable]
public sealed record UpdateRolePermissionsCommand : IRequest<CommonActionResultModel>
{
    public required Guid RoleId { get; init; }
    public IList<Guid> PermissionIds { get; init; } = [];
}
