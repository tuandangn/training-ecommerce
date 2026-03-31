namespace NamEcommerce.Domain.Shared.Common;

public interface INameExistCheckingService
{
    Task<bool> DoesNameExistAsync(string name, Guid? comparesWithCurrentId = null);
}
