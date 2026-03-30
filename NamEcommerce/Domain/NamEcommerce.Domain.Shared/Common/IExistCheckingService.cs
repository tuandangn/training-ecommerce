namespace NamEcommerce.Domain.Shared.Common;

public interface IExistCheckingService
{
    Task<bool> DoesNameExistAsync(string name, Guid? comparesWithCurrentId = null);
}
