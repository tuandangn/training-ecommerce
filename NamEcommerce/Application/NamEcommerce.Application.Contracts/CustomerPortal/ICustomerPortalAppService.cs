using NamEcommerce.Application.Contracts.Dtos.CustomerPortal;

namespace NamEcommerce.Application.Contracts.CustomerPortal;

public interface ICustomerPortalAppService
{
    Task<PublicDeliveryNoteAppDto?> GetPublicDeliveryNoteByTokenAsync(string token);
    Task<CustomerDashboardAppDto> GetDashboardAsync(Guid customerId);
    Task<IReadOnlyCollection<CustomerOrderSummaryAppDto>> GetOrdersAsync(Guid customerId);
    Task<CustomerOrderDetailsAppDto?> GetOrderDetailsAsync(Guid customerId, Guid orderId);
    Task<CustomerOrderRequestAppDto> CreateOrderRequestAsync(Guid customerId, CreateCustomerOrderRequestAppDto dto);
    Task<IReadOnlyCollection<CustomerDeliveryNoteSummaryAppDto>> GetDeliveryNotesAsync(Guid customerId);
    Task<CustomerDeliveryNoteDetailsAppDto?> GetDeliveryNoteDetailsAsync(Guid customerId, Guid deliveryNoteId);
    Task<CustomerActionResultAppDto> ConfirmDeliveryNoteAsync(Guid customerId, Guid deliveryNoteId, ConfirmCustomerDeliveryNoteAppDto dto);
    Task<CustomerActionResultAppDto> CreateDeliveryFeedbackAsync(Guid customerId, CreateCustomerDeliveryFeedbackAppDto dto);
    Task<CustomerReturnRequestAppDto> CreateReturnRequestAsync(Guid customerId, CreateCustomerReturnRequestAppDto dto);
    Task<CustomerDebtSummaryPortalAppDto> GetDebtSummaryAsync(Guid customerId);
}
