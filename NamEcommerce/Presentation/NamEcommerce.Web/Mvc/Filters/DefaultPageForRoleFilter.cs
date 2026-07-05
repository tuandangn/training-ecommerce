using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using NamEcommerce.Application.Contracts.Users;
using NamEcommerce.Domain.Shared.Common;

namespace NamEcommerce.Web.Mvc.Filters;

public sealed class DefaultPageForRoleFilter : IAsyncActionFilter
{
    private readonly IUserAppService _userAppService;
    private readonly ICurrentUserService _currentUserService;

    public DefaultPageForRoleFilter(IUserAppService userAppService, ICurrentUserService currentUserService)
    {
        _userAppService = userAppService;
        _currentUserService = currentUserService;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var controller = context.RouteData.Values["controller"]?.ToString();
        var action = context.RouteData.Values["action"]?.ToString();

        var isHomeIndex = string.Equals(controller, "Home", StringComparison.OrdinalIgnoreCase)
                         && string.Equals(action, "Index", StringComparison.OrdinalIgnoreCase);

        if (!isHomeIndex)
        {
            await next();
            return;
        }

        var currentUser = await _currentUserService.GetCurrentUserInfoAsync();
        if (currentUser is null)
        {
            await next();
            return;
        }

        if (await _userAppService.IsUserInRoleAsync(currentUser.Id, SystemUserRoleNames.Admin))
        {
            await next();
            return;
        }

        if (await _userAppService.IsUserInRoleAsync(currentUser.Id, SystemUserRoleNames.WarehouseManager))
        {
            context.Result = new RedirectToActionResult("Index", "DeliveryRun", null);
            return;
        }

        if (await _userAppService.IsUserInRoleAsync(currentUser.Id, SystemUserRoleNames.DeliveryStaff))
        {
            context.Result = new RedirectToActionResult("Index", "DeliveryMobile", null);
            return;
        }

        await next();
    }
}
