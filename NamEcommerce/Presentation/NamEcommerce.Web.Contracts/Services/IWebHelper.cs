namespace NamEcommerce.Web.Contracts.Services;


public interface IWebHelper
{
    bool IsMatchRouteInfo(string controller, string? action = null);

    string EncodeUrlComponent(string urlComponent);
}
