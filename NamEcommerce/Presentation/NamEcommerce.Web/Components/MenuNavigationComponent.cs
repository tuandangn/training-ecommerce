using Microsoft.AspNetCore.Mvc;

namespace NamEcommerce.Web.Components;

public sealed class MenuNavigationComponent : ViewComponent
{
    public IViewComponentResult Invoke() => View();
}
