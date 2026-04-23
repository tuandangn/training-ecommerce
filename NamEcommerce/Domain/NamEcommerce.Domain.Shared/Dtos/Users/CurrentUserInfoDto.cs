namespace NamEcommerce.Domain.Shared.Dtos.Users;

[Serializable]
public sealed record CurrentUserInfoDto(Guid Id, string Username, string FullName);
