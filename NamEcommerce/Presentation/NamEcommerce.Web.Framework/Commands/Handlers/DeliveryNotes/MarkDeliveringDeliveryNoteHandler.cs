using MediatR;
using NamEcommerce.Application.Contracts.DeliveryNotes;
using NamEcommerce.Web.Contracts.Commands.Models.DeliveryNotes;

namespace NamEcommerce.Web.Framework.Commands.Handlers.DeliveryNotes;

public sealed class MarkDeliveringDeliveryNoteHandler : IRequestHandler<MarkDeliveringDeliveryNoteCommand, Unit>
{
    private readonly IDeliveryNoteAppService _deliveryNoteAppService;

    public MarkDeliveringDeliveryNoteHandler(IDeliveryNoteAppService deliveryNoteAppService)
    {
        _deliveryNoteAppService = deliveryNoteAppService;
    }

    public async Task<Unit> Handle(MarkDeliveringDeliveryNoteCommand request, CancellationToken cancellationToken)
    {
        await _deliveryNoteAppService.MarkDeliveringAsync(request.DeliveryNoteId).ConfigureAwait(false);
        return Unit.Value;
    }
}
