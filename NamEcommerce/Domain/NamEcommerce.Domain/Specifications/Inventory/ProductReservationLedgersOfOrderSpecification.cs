using NamEcommerce.Domain.Entities.Inventory;
using NamEcommerce.Domain.Shared.Specifications;

namespace NamEcommerce.Domain.Specifications.Inventory;

[Serializable]
public sealed class ProductReservationLedgersOfOrderSpecification(Guid productId, Guid orderId)
    : BaseSpecification<ProductReservationLedger>(entry => entry.ProductId == productId && entry.OrderId == orderId);
