namespace NamEcommerce.Web.Services.Common;

public interface ICachedValuesService
{
    Guid DefaultCustomerId { get; }
    void SetDefaultCustomerId(Guid customerId);
}
