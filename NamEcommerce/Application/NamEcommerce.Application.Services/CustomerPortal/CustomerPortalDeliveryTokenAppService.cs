using NamEcommerce.Application.Contracts.CustomerPortal;
using NamEcommerce.Application.Contracts.DeliveryNotes;
using NamEcommerce.Application.Contracts.Dtos.CustomerPortal;
using NamEcommerce.Domain.Shared.Dtos.CustomerPortal;
using NamEcommerce.Domain.Shared.Services.CustomerPortal;

namespace NamEcommerce.Application.Services.CustomerPortal;

public sealed class CustomerPortalDeliveryTokenAppService(
    ICustomerPortalSecurityManager securityManager,
    IDeliveryNoteAppService deliveryNoteAppService) : ICustomerPortalDeliveryTokenAppService
{
    public async Task<CustomerPortalDeliveryAccessTokenAppDto?> CreateDeliveryAccessTokenAsync(Guid deliveryNoteId)
    {
        if (deliveryNoteId == Guid.Empty)
            return null;

        var deliveryNote = await deliveryNoteAppService.GetByIdAsync(deliveryNoteId).ConfigureAwait(false);
        if (deliveryNote is null)
            return null;

        var token = CustomerPortalHashing.CreateSecureToken();
        await securityManager.CreateDeliveryNoteAccessTokenAsync(new CreateDeliveryNoteAccessTokenDto
        {
            DeliveryNoteId = deliveryNoteId,
            TokenHash = CustomerPortalHashing.Hash(token)
        }).ConfigureAwait(false);

        return new CustomerPortalDeliveryAccessTokenAppDto
        {
            DeliveryNoteId = deliveryNoteId,
            Token = token
        };
    }
}
