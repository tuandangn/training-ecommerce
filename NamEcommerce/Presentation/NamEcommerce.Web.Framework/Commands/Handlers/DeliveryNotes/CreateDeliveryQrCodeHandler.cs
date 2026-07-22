using MediatR;
using NamEcommerce.Application.Contracts.CustomerPortal;
using NamEcommerce.Application.Contracts.DeliveryNotes;
using NamEcommerce.Web.Contracts.Commands.Models.DeliveryNotes;
using NamEcommerce.Web.Contracts.Models.DeliveryNotes;
using NamEcommerce.Web.Framework.Services.QrCode;

namespace NamEcommerce.Web.Framework.Commands.Handlers.DeliveryNotes;

public sealed class CreateDeliveryQrCodeHandler(IDeliveryNoteAppService deliveryNoteAppService, ICustomerPortalDeliveryTokenAppService tokenAppService) 
    : IRequestHandler<CreateDeliveryQrCodeCommand, CustomerPortalDeliveryQrCodeModel?>
{
    public async Task<CustomerPortalDeliveryQrCodeModel?> Handle(CreateDeliveryQrCodeCommand request, CancellationToken cancellationToken)
    {
        var deliverNote = await deliveryNoteAppService.GetByIdAsync(request.DeliveryNoteId).ConfigureAwait(false);
        if (deliverNote is null)
            return null;

        var token = await tokenAppService.CreateDeliveryAccessTokenAsync(request.DeliveryNoteId).ConfigureAwait(false);
        if (token is null)
            return null;

        var url = $"{request.CustomerPortalUrl}/d/{token.Token}";
        return new CustomerPortalDeliveryQrCodeModel(url, QrCodeSvgRenderer.Render(url));

    }
}
