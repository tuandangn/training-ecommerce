using MediatR;
using NamEcommerce.Web.Contracts.Models.Users;

namespace NamEcommerce.Web.Contracts.Queries.Models.Users;

[Serializable]
public sealed record GetAdminUserListQuery : IRequest<AdminUserListModel>;

[Serializable]
public sealed record GetChangeUserPasswordModelQuery(Guid UserId) : IRequest<ChangeUserPasswordModel?>;
