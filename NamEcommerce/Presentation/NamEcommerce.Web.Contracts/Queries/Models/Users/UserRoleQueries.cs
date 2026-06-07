using MediatR;
using NamEcommerce.Web.Contracts.Models.Users;

namespace NamEcommerce.Web.Contracts.Queries.Models.Users;

[Serializable]
public sealed record GetUserRoleListQuery : IRequest<UserRoleListModel>;

[Serializable]
public sealed record GetUserRoleAssignmentQuery(Guid UserId) : IRequest<UserRoleAssignmentModel?>;
