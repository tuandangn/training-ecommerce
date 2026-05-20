using Microsoft.AspNetCore.Http;

namespace NamEcommerce.Web.Services.CustomerPortal;

public interface ICustomerPortalQrCodeService
{
    Task<CustomerPortalDeliveryQrCodeModel?> CreateDeliveryQrCodeAsync(Guid deliveryNoteId, HttpRequest request);
}

public sealed record CustomerPortalDeliveryQrCodeModel(string Url, string Svg);
