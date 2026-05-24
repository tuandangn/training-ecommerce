using NamEcommerce.Web.Models.E2E;

namespace NamEcommerce.Web.Services.E2E;

public interface IE2ETestDataService
{
    Task ResetAsync(string? scenarioId, CancellationToken cancellationToken = default);

    Task<E2ESeedOrderWorkflowResult> SeedOrderWorkflowAsync(
        E2ESeedOrderWorkflowRequest request,
        CancellationToken cancellationToken = default);

    Task<E2EOrderWorkflowState> GetOrderWorkflowStateAsync(
        string scenarioId,
        CancellationToken cancellationToken = default);
}
