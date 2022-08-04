namespace NamEcommerce.Domain.Shared.Services;

public interface IUserManager
{
    Task<bool> DoesUsernameExistAsync(string username, Guid? comparesWithCurrentId = null);
}
