using NamEcommerce.Application.Contracts.Dtos.CustomerPortal;

namespace NamEcommerce.Application.Contracts.CustomerPortal;

public interface ICustomerPortalDeliveryTokenAppService
{
    Task<CustomerPortalDeliveryAccessTokenAppDto?> CreateDeliveryAccessTokenAsync(Guid deliveryNoteId);
}
