namespace NamEcommerce.Application.Contracts.Dtos.Users;

[Serializable]
public sealed record CurrentUserInfoAppDto(Guid Id, string Username, string FullName);
