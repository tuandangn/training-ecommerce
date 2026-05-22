using NamEcommerce.Customer.Contracts.Models;

namespace NamEcommerce.Customer.Framework.Services;

public interface ICustomerSessionAccessor
{
    Guid? CustomerId { get; }
    Guid? SessionId { get; }
    bool IsAuthenticated { get; }
    CustomerSessionModel? CurrentSession { get; }
}
