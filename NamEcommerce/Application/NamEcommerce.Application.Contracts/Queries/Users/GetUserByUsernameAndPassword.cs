using MediatR;
using NamEcommerce.Application.Contracts.Dtos.Users;

namespace NamEcommerce.Application.Contracts.Queries.Users;

[Serializable]
public sealed record GetUserByUsernameAndPassword(string Username, string Password) : IRequest<UserAppDto?>;

