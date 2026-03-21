using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using NamEcommerce.Web.Contracts.Services;

namespace NamEcommerce.Web.Framework.Services;

public sealed class WebHelper : IWebHelper
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public WebHelper(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public bool IsMatchRouteInfo(string controller, string? action = null)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext is null)
            return false;

        var routeData = httpContext.GetRouteData();
        if (routeData is null)
            return false;

        var currentController = routeData.Values["controller"]?.ToString();
        var isControllerMatched = string.Equals(currentController, controller, StringComparison.OrdinalIgnoreCase);
        if (string.IsNullOrEmpty(action))
            return isControllerMatched;

        var currentAction = routeData.Values["action"]?.ToString();
        var isActionMatched = string.Equals(currentAction, action, StringComparison.OrdinalIgnoreCase);

        return isControllerMatched && isActionMatched;
    }
}
