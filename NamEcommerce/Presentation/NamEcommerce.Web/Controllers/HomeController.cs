using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NamEcommerce.Web.Models;
using NamEcommerce.Web.Services.Dashboard;
using System.Diagnostics;

namespace NamEcommerce.Web.Controllers;

public sealed class HomeController : BaseController
{
    private readonly IDashboardModelFactory _dashboardModelFactory;

    public HomeController(IDashboardModelFactory dashboardModelFactory)
    {
        _dashboardModelFactory = dashboardModelFactory;
    }

    [Authorize]
    public async Task<IActionResult> Index()
    {
        var model = await _dashboardModelFactory.PrepareDashboardModelAsync().ConfigureAwait(false);
        return View(model);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
