using MediatR;

namespace NamEcommerce.Web.Contracts.Queries.Models.Users;

[Serializable]
public sealed record CanManageUserRolesQuery : IRequest<bool>;
