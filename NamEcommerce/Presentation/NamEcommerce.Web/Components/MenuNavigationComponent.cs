using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NamEcommerce.Web.Contracts.Security;
using NamEcommerce.Web.Models.Common;

namespace NamEcommerce.Web.Components;

public sealed class MenuNavigationComponent(IAuthorizationService authorizationService) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync()
    {
        var user = HttpContext.User;
        var model = new MenuNavigationModel
        {
            CanManageUserRoles = (await authorizationService.AuthorizeAsync(user, SystemPermissions.Users.ManageRoles).ConfigureAwait(false)).Succeeded,
            CanViewDeliveryRuns = (await authorizationService.AuthorizeAsync(user, SystemPermissions.DeliveryRuns.View).ConfigureAwait(false)).Succeeded,
            CanUseDeliveryMobile = (await authorizationService.AuthorizeAsync(user, SystemPermissions.DeliveryRuns.MobileAccess).ConfigureAwait(false)).Succeeded,
        };

        return View(model);
    }
}
