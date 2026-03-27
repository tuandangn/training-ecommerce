using MediatR;
using NamEcommerce.Application.Contracts.Dtos.Users;

namespace NamEcommerce.Application.Contracts.Commands.Models;

[Serializable]
public sealed record CreateUserCommand(CreateUserAppDto Dto) : IRequest<CreateUserResultAppDto>;
