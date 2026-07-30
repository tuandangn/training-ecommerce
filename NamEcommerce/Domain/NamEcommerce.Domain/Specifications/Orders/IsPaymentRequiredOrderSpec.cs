using NamEcommerce.Domain.Entities.Orders;
using NamEcommerce.Domain.Shared.Specifications;

namespace NamEcommerce.Domain.Specifications.Orders;

[Serializable]
public sealed class IsPaymentRequiredOrderSpec() : BaseSpecification<Order>(order => order.ProcessRequiresPayment 
    && ((order.PayOffRequired && order.PaidAmount < order.OrderTotal)
        || order.PaidAmount == null
));
