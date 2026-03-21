using MediatR;

namespace NamEcommerce.Application.Contracts.Queries.Users;

[Serializable]
public sealed record DoesUsernameExist(string Username) : IRequest<bool>;
