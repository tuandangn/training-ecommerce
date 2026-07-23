using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NamEcommerce.Application.Contracts.Users;
using NamEcommerce.Web.Contracts.Configurations;

namespace NamEcommerce.Web.Components;

public sealed class AdminResetDataComponent(ICurrentUserService currentUserService, AppConfig appConfig) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync()
    {
        if(!appConfig.AllowAdminResetData)
            return Content(string.Empty);

        if (!await currentUserService.IsAdminAsync())
            return Content(string.Empty);

        return View();
    }
}
