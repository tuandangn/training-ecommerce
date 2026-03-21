namespace NamEcommerce.Web.Contracts.Dtos;

[Serializable]
public sealed record CurrentUserInfoDto(Guid Id, string Username, string FullName);
