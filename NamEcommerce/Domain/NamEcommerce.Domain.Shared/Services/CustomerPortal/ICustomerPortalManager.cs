using NamEcommerce.Domain.Shared.Dtos.CustomerPortal;
using NamEcommerce.Domain.Shared.Enums.CustomerPortal;

namespace NamEcommerce.Domain.Shared.Services.CustomerPortal;

public interface ICustomerPortalManager
{
    Task<CustomerDeliveryFeedbackDto> CreateDeliveryFeedbackAsync(CreateCustomerDeliveryFeedbackDto dto);
    Task<IReadOnlyCollection<CustomerDeliveryFeedbackDto>> GetDeliveryFeedbacksAsync(Guid customerId);

    Task<CustomerOrderRequestDto> CreateOrderRequestAsync(CreateCustomerOrderRequestDto dto);
    Task<CustomerOrderRequestDto?> GetOrderRequestByIdAsync(Guid id);
    Task<IReadOnlyCollection<CustomerOrderRequestDto>> GetOrderRequestsAsync(Guid customerId);
    Task<IReadOnlyCollection<CustomerOrderRequestDto>> GetOrderRequestsByStatusAsync(CustomerOrderRequestStatus status);
    Task ApproveOrderRequestAsync(Guid id, Guid reviewedByUserId, IReadOnlyDictionary<Guid, decimal> itemPrices, string? adminNote, DateTime nowUtc);
    Task RejectOrderRequestAsync(Guid id, Guid reviewedByUserId, string? adminNote, DateTime nowUtc);
    Task MarkOrderRequestConvertedAsync(Guid id, Guid orderId, DateTime nowUtc);

    Task<CustomerReturnRequestDto> CreateReturnRequestAsync(CreateCustomerReturnRequestDto dto);
    Task<CustomerReturnRequestDto?> GetReturnRequestByIdAsync(Guid id);
    Task<IReadOnlyCollection<CustomerReturnRequestDto>> GetReturnRequestsAsync(Guid customerId);
    Task<IReadOnlyCollection<CustomerReturnRequestDto>> GetReturnRequestsByStatusAsync(CustomerReturnRequestStatus status);
    Task AcceptReturnRequestAsync(Guid id, Guid reviewedByUserId, string? adminNote, DateTime nowUtc);
    Task RejectReturnRequestAsync(Guid id, Guid reviewedByUserId, string? adminNote, DateTime nowUtc);
    Task MarkReturnRequestConvertedAsync(Guid id, Guid customerReturnId, DateTime nowUtc);

    Task<CustomerPaymentIntentDto> CreatePaymentIntentAsync(CreateCustomerPaymentIntentDto dto);
    Task<CustomerPaymentIntentDto?> GetPaymentIntentByIdAsync(Guid id);
    Task<IReadOnlyCollection<CustomerPaymentIntentDto>> GetPaymentIntentsAsync(Guid customerId);
    Task<IReadOnlyCollection<CustomerPaymentIntentDto>> GetPaymentIntentsByStatusAsync(CustomerPaymentIntentStatus status);
    Task<CustomerPaymentIntentDto> MarkPaymentIntentProcessingAsync(Guid id, string providerIntentId);
    Task<CustomerPaymentIntentDto> MarkPaymentIntentSucceededPendingReconciliationAsync(Guid id, DateTime nowUtc);
    Task<CustomerPaymentIntentDto> MarkPaymentIntentFailedAsync(Guid id, string? failureReason, DateTime nowUtc);
    Task MarkPaymentIntentReconciledAsync(Guid id, Guid customerPaymentId, Guid reconciledByUserId, DateTime nowUtc);
}
