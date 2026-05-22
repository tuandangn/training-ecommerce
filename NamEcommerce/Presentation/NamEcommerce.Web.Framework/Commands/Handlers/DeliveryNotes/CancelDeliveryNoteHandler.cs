using MediatR;
using NamEcommerce.Application.Contracts.DeliveryNotes;
using NamEcommerce.Web.Contracts.Commands.Models.DeliveryNotes;

namespace NamEcommerce.Web.Framework.Commands.Handlers.DeliveryNotes;

public sealed class CancelDeliveryNoteHandler : IRequestHandler<CancelDeliveryNoteCommand, Unit>
{
    private readonly IDeliveryNoteAppService _deliveryNoteAppService;

    public CancelDeliveryNoteHandler(IDeliveryNoteAppService deliveryNoteAppService)
    {
        _deliveryNoteAppService = deliveryNoteAppService;
    }

    public async Task<Unit> Handle(CancelDeliveryNoteCommand request, CancellationToken cancellationToken)
    {
        await _deliveryNoteAppService.CancelAsync(request.DeliveryNoteId).ConfigureAwait(false);
        return Unit.Value;
    }
}
