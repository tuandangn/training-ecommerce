using NamEcommerce.Web.Contracts.Services;

namespace NamEcommerce.Web.Contracts.Extensions;

public static class WebHelperExtensions
{
    public static bool IsLoginPage(this IWebHelper webHelper) 
        => webHelper.IsMatchRouteInfo("User", "Login");

    public static bool IsRegisterPage(this IWebHelper webHelper) 
        => webHelper.IsMatchRouteInfo("User", "Register");

    public static bool IsHasController(this IWebHelper webHelper, params string[] controllers)
    {
        if (controllers == null)
            return false;

        foreach (var controller in controllers)
        {
            if (webHelper.IsMatchRouteInfo(controller))
                return true;
        }

        return false;
    }
}
