using Microsoft.AspNetCore.Mvc;
using NamEcommerce.Web.Constants;

namespace NamEcommerce.Web.Controllers;

public sealed class DesignController : BaseController
{
    private readonly IWebHostEnvironment _environment;

    public DesignController(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    [HttpGet("design")]
    public IActionResult Index()
    {
        if (!_environment.IsDevelopment())
            return NotFound();

        ViewData[ViewConstants.PageTitle] = "Design System";
        return View();
    }
}
