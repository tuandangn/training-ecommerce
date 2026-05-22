using NamEcommerce.Domain.Shared.Dtos.PurchaseOrders;

namespace NamEcommerce.Domain.Shared.Services.PurchaseOrders;

public interface ISupplierSuggestionService
{
    Task<IList<SupplierSuggestionDto>> SuggestVendorsForProductAsync(Guid productId);
}

