namespace NamEcommerce.Application.Contracts.Security;

public interface IAuthorizationAppService
{
    Task<bool> Authorize(Guid userId, string permissionName);
}
