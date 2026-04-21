using MediatR;
using NamEcommerce.Application.Contracts.DeliveryNotes;
using NamEcommerce.Web.Contracts.Commands.Models.DeliveryNotes;
using NamEcommerce.Web.Contracts.Models.Common;

namespace NamEcommerce.Web.Framework.Commands.Handlers.DeliveryNotes;

public sealed class MarkDeliveringDeliveryNoteHandler : IRequestHandler<MarkDeliveringDeliveryNoteCommand, CommonActionResultModel>
{
    private readonly IDeliveryNoteAppService _deliveryNoteAppService;

    public MarkDeliveringDeliveryNoteHandler(IDeliveryNoteAppService deliveryNoteAppService)
    {
        _deliveryNoteAppService = deliveryNoteAppService;
    }

    public async Task<CommonActionResultModel> Handle(MarkDeliveringDeliveryNoteCommand request, CancellationToken cancellationToken)
    {
        try
        {
            await _deliveryNoteAppService.MarkDeliveringAsync(request.DeliveryNoteId).ConfigureAwait(false);
            return new CommonActionResultModel
            {
                Success = true
            };
        }
        catch (Exception ex)
        {
            return new CommonActionResultModel
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }
}
