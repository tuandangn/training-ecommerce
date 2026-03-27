namespace NamEcommerce.Domain.Shared.Common;

public interface ICheckNameService
{
    Task<bool> DoesNameExistAsync(string name, Guid? comparesWithCurrentId = null);
}
