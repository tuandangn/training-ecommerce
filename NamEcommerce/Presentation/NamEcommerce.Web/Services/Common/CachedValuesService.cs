namespace NamEcommerce.Web.Services.Common;

public sealed class CachedValuesService : ICachedValuesService
{
    private Guid defaultCustomerId;
    public Guid DefaultCustomerId => defaultCustomerId;

    public void SetDefaultCustomerId(Guid customerId)
    {
        defaultCustomerId = customerId;
    }
}
