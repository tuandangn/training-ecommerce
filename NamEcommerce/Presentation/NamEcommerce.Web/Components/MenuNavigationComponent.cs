using MediatR;
using Microsoft.AspNetCore.Mvc;
using NamEcommerce.Domain.Shared.Dtos.Users;
using NamEcommerce.Web.Contracts.Queries.Models.Users;
using NamEcommerce.Web.Models.Common;

namespace NamEcommerce.Web.Components;

public sealed class MenuNavigationComponent(IMediator mediator) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync()
    {
        var user = HttpContext.User;
        var model = new MenuNavigationModel
        {
            CanManageUserRoles = await mediator.Send(new CanManageUserRolesQuery()).ConfigureAwait(false),
            CanViewDeliveryRuns = user.IsInRole(SystemUserRoleNames.Admin)
                || user.IsInRole(SystemUserRoleNames.WarehouseManager)
                || user.IsInRole(SystemUserRoleNames.Cashier),
            CanUseDeliveryMobile = user.IsInRole(SystemUserRoleNames.DeliveryStaff)
        };

        return View(model);
    }
}
