using Microsoft.AspNetCore.Mvc;
using NamEcommerce.Data.SqlServer;
using NamEcommerce.Web.Models.E2E;
using NamEcommerce.Web.Services.E2E;

namespace NamEcommerce.Web.Controllers;

[ApiController]
[Route("__e2e")]
public sealed class E2ETestController : ControllerBase
{
    private readonly IWebHostEnvironment _environment;
    private readonly IConfiguration _configuration;
    private readonly IServiceProvider _serviceProvider;

    public E2ETestController(
        IWebHostEnvironment environment,
        IConfiguration configuration,
        IServiceProvider serviceProvider)
    {
        _environment = environment;
        _configuration = configuration;
        _serviceProvider = serviceProvider;
    }

    [HttpPost("reset")]
    public async Task<IActionResult> Reset(E2EResetRequest request, CancellationToken cancellationToken)
    {
        if (!IsAllowed())
            return NotFound();

        await GetTestDataService().ResetAsync(request.ScenarioId, cancellationToken).ConfigureAwait(false);
        return Ok(new { success = true });
    }

    [HttpPost("seed/order-workflow")]
    public async Task<IActionResult> SeedOrderWorkflow(E2ESeedOrderWorkflowRequest request, CancellationToken cancellationToken)
    {
        if (!IsAllowed())
            return NotFound();

        var result = await GetTestDataService().SeedOrderWorkflowAsync(request, cancellationToken).ConfigureAwait(false);
        return Ok(result);
    }

    [HttpGet("state/order-workflow/{scenarioId}")]
    public async Task<IActionResult> GetOrderWorkflowState(string scenarioId, CancellationToken cancellationToken)
    {
        if (!IsAllowed())
            return NotFound();

        var result = await GetTestDataService().GetOrderWorkflowStateAsync(scenarioId, cancellationToken).ConfigureAwait(false);
        return Ok(result);
    }

    [HttpGet("state/inventory-stock/{scenarioId}")]
    public async Task<IActionResult> GetInventoryStockState(string scenarioId, CancellationToken cancellationToken)
    {
        if (!IsAllowed())
            return NotFound();

        var result = await GetTestDataService().GetInventoryStockStateAsync(scenarioId, cancellationToken).ConfigureAwait(false);
        return Ok(result);
    }

    private IE2ETestDataService GetTestDataService()
        => _serviceProvider.GetRequiredService<IE2ETestDataService>();

    private bool IsAllowed()
    {
        if (!_environment.IsEnvironment("E2E"))
            return false;

        if (!_configuration.GetValue<bool>("E2E:Enabled"))
            return false;

        var expectedToken = _configuration["E2E:Token"];
        if (string.IsNullOrWhiteSpace(expectedToken))
            return false;

        if (!Request.Headers.TryGetValue("X-E2E-Token", out var actualToken) || actualToken != expectedToken)
            return false;

        var requiredDbFragment = _configuration["E2E:RequiredDatabaseNameFragment"];
        var connectionString = _configuration.GetConnectionString(nameof(NamEcommerceEfDbContext));

        return string.IsNullOrWhiteSpace(requiredDbFragment)
            || (connectionString?.Contains(requiredDbFragment, StringComparison.OrdinalIgnoreCase) ?? false);
    }
}
