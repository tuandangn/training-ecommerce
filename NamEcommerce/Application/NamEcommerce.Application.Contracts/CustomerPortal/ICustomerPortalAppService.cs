using NamEcommerce.Application.Contracts.Dtos.CustomerPortal;

namespace NamEcommerce.Application.Contracts.CustomerPortal;

public interface ICustomerPortalAppService
{
    Task<PublicDeliveryNoteAppDto?> GetPublicDeliveryNoteByTokenAsync(string token);
    Task<CustomerDashboardAppDto> GetDashboardAsync(Guid customerId);
    Task<IReadOnlyCollection<CustomerOrderSummaryAppDto>> GetOrdersAsync(Guid customerId);
    Task<CustomerOrderDetailsAppDto?> GetOrderDetailsAsync(Guid customerId, Guid orderId);
    Task<CustomerOrderRequestAppDto> CreateOrderRequestAsync(Guid customerId, CreateCustomerOrderRequestAppDto dto);
    Task<IReadOnlyCollection<CustomerOrderRequestSummaryAppDto>> GetOrderRequestsAsync(Guid customerId);
    Task<CustomerOrderRequestDetailsAppDto?> GetOrderRequestDetailsAsync(Guid customerId, Guid orderRequestId);
    Task<CustomerPortalConversionResultAppDto> ConfirmOrderRequestAsync(Guid customerId, Guid orderRequestId);
    Task<CustomerOrderRequestDefaultsAppDto> GetOrderRequestDefaultsAsync(Guid customerId);
    Task<CustomerProductListAppDto> GetProductsAsync(Guid customerId, Guid? categoryId, string? keywords, bool purchasedOnly, int pageSize);
    Task<CustomerProductCategoryListAppDto> GetProductCategoriesAsync();
    Task<CustomerContactAppDto> GetContactAsync();
    Task<IReadOnlyCollection<CustomerDeliveryNoteSummaryAppDto>> GetDeliveryNotesAsync(Guid customerId);
    Task<CustomerDeliveryNoteDetailsAppDto?> GetDeliveryNoteDetailsAsync(Guid customerId, Guid deliveryNoteId);
    Task<CustomerActionResultAppDto> ConfirmDeliveryNoteAsync(Guid customerId, Guid deliveryNoteId, ConfirmCustomerDeliveryNoteAppDto dto);
    Task<CustomerActionResultAppDto> CreateDeliveryFeedbackAsync(Guid customerId, CreateCustomerDeliveryFeedbackAppDto dto);
    Task<IReadOnlyCollection<CustomerReturnableItemAppDto>> GetReturnableItemsAsync(Guid customerId);
    Task<CustomerReturnRequestAppDto> CreateReturnRequestAsync(Guid customerId, CreateCustomerReturnRequestAppDto dto);
    Task<IReadOnlyCollection<CustomerReturnRequestSummaryAppDto>> GetReturnRequestsAsync(Guid customerId);
    Task<CustomerReturnRequestDetailsAppDto?> GetReturnRequestDetailsAsync(Guid customerId, Guid returnRequestId);
    Task<CustomerActionResultAppDto> CancelReturnRequestAsync(Guid customerId, Guid returnRequestId);
    Task<CustomerDebtSummaryPortalAppDto> GetDebtSummaryAsync(Guid customerId);
}
