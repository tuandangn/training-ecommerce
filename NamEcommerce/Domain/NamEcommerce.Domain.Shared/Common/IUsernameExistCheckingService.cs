namespace NamEcommerce.Domain.Shared.Common;

public interface IUsernameExistCheckingService
{
    Task<bool> DoesUsernameExistAsync(string username, Guid? comparesWithCurrentId = null);
}
