using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NamEcommerce.Domain.Shared.Services.Users;
using NamEcommerce.Web.Contracts.Security;
using NamEcommerce.Web.Models.Common;

namespace NamEcommerce.Web.Components;

public sealed class UserHeaderLinksComponent(
    ICurrentUserAccessor currentUserAccessor,
    IAuthorizationService authorizationService) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync()
    {
        var currentUser = await currentUserAccessor.GetCurrentUserAsync().ConfigureAwait(false);
        var model = new UserHeaderLinksModel
        {
            IsAuthenticated = currentUser is not null
        };
        if (!model.IsAuthenticated)
            return View(model);

        model = model with
        {
            UserId = currentUser!.Id,
            FullName = currentUser.FullName,
            Username = currentUser.Username,
            CanViewOrderFulfillmentSchedule = (await authorizationService
                .AuthorizeAsync(HttpContext.User, SystemPermissions.Orders.View)
                .ConfigureAwait(false)).Succeeded
        };

        return View(model);
    }
}
