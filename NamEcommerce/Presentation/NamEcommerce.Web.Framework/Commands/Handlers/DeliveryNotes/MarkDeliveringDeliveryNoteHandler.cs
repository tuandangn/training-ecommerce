using MediatR;
using NamEcommerce.Application.Contracts.DeliveryNotes;
using NamEcommerce.Web.Contracts.Commands.Models.DeliveryNotes;
using NamEcommerce.Web.Contracts.Models.Common;

namespace NamEcommerce.Web.Framework.Commands.Handlers.DeliveryNotes;

public sealed class MarkDeliveringDeliveryNoteHandler : IRequestHandler<MarkDeliveringDeliveryNoteCommand, CommonResultModel>
{
    private readonly IDeliveryNoteAppService _deliveryNoteAppService;

    public MarkDeliveringDeliveryNoteHandler(IDeliveryNoteAppService deliveryNoteAppService)
    {
        _deliveryNoteAppService = deliveryNoteAppService;
    }

    public async Task<CommonResultModel> Handle(MarkDeliveringDeliveryNoteCommand request, CancellationToken cancellationToken)
    {
        try
        {
            await _deliveryNoteAppService.MarkDeliveringAsync(request.DeliveryNoteId).ConfigureAwait(false);
            return new CommonResultModel
            {
                Success = true
            };
        }
        catch (Exception ex)
        {
            return new CommonResultModel
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }
}
