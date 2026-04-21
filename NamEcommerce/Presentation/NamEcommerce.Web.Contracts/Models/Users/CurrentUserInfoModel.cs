namespace NamEcommerce.Web.Contracts.Models.Users;

[Serializable]
public sealed record CurrentUserInfoModel(Guid Id, string Username, string FullName);
