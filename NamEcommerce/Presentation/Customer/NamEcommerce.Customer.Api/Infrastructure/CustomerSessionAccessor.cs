using NamEcommerce.Customer.Contracts.Models;
using NamEcommerce.Customer.Framework.Services;

namespace NamEcommerce.Customer.Api.Infrastructure;

internal sealed class CustomerSessionAccessor(IHttpContextAccessor httpContextAccessor) : ICustomerSessionAccessor
{
    public CustomerSessionModel? CurrentSession
        => httpContextAccessor.HttpContext?.Items[CustomerPortalAuthDefaults.SessionItemKey] as CustomerSessionModel;

    public Guid? CustomerId => CurrentSession?.CustomerId;
    public Guid? SessionId => CurrentSession?.SessionId;
    public bool IsAuthenticated => CurrentSession is not null;
}
