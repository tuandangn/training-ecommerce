namespace NamEcommerce.Domain.Shared.Common;

public interface ICodeExistCheckingService
{
    Task<bool> DoesCodeExistAsync(string code, Guid? comparesWithCurrentId = null);
}
