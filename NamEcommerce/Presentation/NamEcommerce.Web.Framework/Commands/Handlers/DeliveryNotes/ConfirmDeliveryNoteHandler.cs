using MediatR;
using NamEcommerce.Application.Contracts.DeliveryNotes;
using NamEcommerce.Application.Contracts.Dtos.Inventory;
using NamEcommerce.Application.Contracts.Inventory;
using NamEcommerce.Application.Contracts.Security;
using NamEcommerce.Application.Contracts.Users;
using NamEcommerce.Web.Contracts.Commands.Models.DeliveryNotes;
using NamEcommerce.Web.Contracts.Models.DeliveryNotes;
using NamEcommerce.Web.Contracts.Models.Inventory;
using NamEcommerce.Web.Contracts.Security;
using NamEcommerce.Web.Framework.Extensions;

namespace NamEcommerce.Web.Framework.Commands.Handlers.DeliveryNotes;

public sealed class ConfirmDeliveryNoteHandler : IRequestHandler<ConfirmDeliveryNoteCommand, ConfirmDeliveryNoteResultModel>
{
    private readonly IDeliveryNoteAppService _deliveryNoteAppService;
    private readonly IShortageAggregationAppService _shortageAggregationAppService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuthorizationAppService _authorizationAppService;

    public ConfirmDeliveryNoteHandler(IDeliveryNoteAppService deliveryNoteAppService,
        IShortageAggregationAppService shortageAggregationAppService, ICurrentUserService currentUserService,
        IAuthorizationAppService authorizationAppService)
    {
        _deliveryNoteAppService = deliveryNoteAppService;
        _shortageAggregationAppService = shortageAggregationAppService;
        _currentUserService = currentUserService;
        _authorizationAppService = authorizationAppService;
    }

    public async Task<ConfirmDeliveryNoteResultModel> Handle(ConfirmDeliveryNoteCommand request, CancellationToken cancellationToken)
    {
        var currentUser = await _currentUserService.GetCurrentUserInfoAsync().ConfigureAwait(false);
        if (currentUser is null)
        {
            return new ConfirmDeliveryNoteResultModel
            {
                Success = false,
                ErrorMessage = "Error.UserNotAuthorized"
            };
        }

        if (!await _currentUserService.IsAdmin() && !await _authorizationAppService.Authorize(currentUser.Id, SystemPermissions.DeliveryNotes.Approve).ConfigureAwait(false))
        {
            return new ConfirmDeliveryNoteResultModel
            {
                Success = false,
                ErrorMessage = "Error.UserNotAuthorized"
            };
        }

        try
        {
            await _deliveryNoteAppService.ConfirmAsync(request.DeliveryNoteId).ConfigureAwait(false);
            return new ConfirmDeliveryNoteResultModel
            {
                Success = true
            };
        }
        catch (Exception ex)
        {
            var shortageItems = Array.Empty<ShortageInfoItemModel>();
            try
            {
                shortageItems = (await _shortageAggregationAppService.GetAggregatedShortagesAsync(new ShortageAggregationFilterAppDto
                {
                    DeliveryNoteId = request.DeliveryNoteId
                }).ConfigureAwait(false)).ToShortageInfoModel().Items.ToArray();
            }
            catch
            {
                shortageItems = [];
            }

            return new ConfirmDeliveryNoteResultModel
            {
                Success = false,
                ErrorMessage = ex.Message,
                ShortageItems = shortageItems
            };
        }
    }
}
