using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace NamEcommerce.Web.Controllers
{
    [Authorize]
    public sealed class PrivateController : Controller
    {
        public IActionResult Index() => View();
    }
}
