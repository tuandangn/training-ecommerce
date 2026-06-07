using MediatR;
using NamEcommerce.Application.Contracts.DeliveryNotes;
using NamEcommerce.Application.Contracts.Dtos.DeliveryNotes;
using NamEcommerce.Web.Contracts.Commands.Models.DeliveryNotes;
using NamEcommerce.Web.Contracts.Models.Common;

namespace NamEcommerce.Web.Framework.Commands.Handlers.DeliveryNotes;

public sealed class AssignDeliveryUserHandler(IDeliveryNoteAppService deliveryNoteAppService)
    : IRequestHandler<AssignDeliveryUserCommand, CommonActionResultModel>
{
    public async Task<CommonActionResultModel> Handle(AssignDeliveryUserCommand request, CancellationToken cancellationToken)
    {
        var result = await deliveryNoteAppService.AssignDeliveryUserAsync(new AssignDeliveryUserAppDto
        {
            DeliveryNoteId = request.DeliveryNoteId,
            AssignedDeliveryUserId = request.AssignedDeliveryUserId
        }).ConfigureAwait(false);

        return new CommonActionResultModel
        {
            Success = result.Success,
            ErrorMessage = result.ErrorMessage,
            SuccessMessage = result.Success ? "Msg.SaveSuccess" : null
        };
    }
}
