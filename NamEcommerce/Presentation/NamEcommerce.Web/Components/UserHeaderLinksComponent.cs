using Microsoft.AspNetCore.Mvc;
using NamEcommerce.Web.Contracts.Services;
using NamEcommerce.Web.Models.Common;

namespace NamEcommerce.Web.Components;

public sealed class UserHeaderLinksComponent : ViewComponent
{
    private readonly ICurrentUserService _currentUserService;

    public UserHeaderLinksComponent(ICurrentUserService currentUserService)
    {
        _currentUserService = currentUserService;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var model = new UserHeaderLinksModel
        {
            IsAuthenticated = await _currentUserService.IsAuthenticatedAsync(),
        };
        if (!model.IsAuthenticated)
            return View(model);

        var userInfo = await _currentUserService.GetCurrentUserInfoAsync();
        if (userInfo is null)
            return View(model);

        model = model with
        {
            UserId = userInfo.Id,
            FullName = userInfo.FullName,
            Username = userInfo.Username,
        };

        return View(model);
    }
}
