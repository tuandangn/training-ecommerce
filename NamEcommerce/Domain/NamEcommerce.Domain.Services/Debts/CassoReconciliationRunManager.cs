using NamEcommerce.Data.Contracts;
using NamEcommerce.Domain.Entities.Debts;
using NamEcommerce.Domain.Shared.Dtos.Debts;
using NamEcommerce.Domain.Shared.Exceptions;
using NamEcommerce.Domain.Shared.Services.Debts;

namespace NamEcommerce.Domain.Services.Debts;

public sealed class CassoReconciliationRunManager(
    IRepository<CassoReconciliationRun> runRepository) : ICassoReconciliationRunManager
{
    public async Task<CassoReconciliationRunDto> StartAsync(StartCassoReconciliationRunDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        dto.Verify();

        var run = new CassoReconciliationRun(dto.FromDate, dto.ToDate, dto.Trigger);
        var inserted = await runRepository.InsertAsync(run).ConfigureAwait(false);
        return MapToDto(inserted);
    }

    public async Task<CassoReconciliationRunDto> CompleteAsync(CompleteCassoReconciliationRunDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var run = await GetRunAsync(dto.RunId).ConfigureAwait(false);
        run.Complete(
            dto.TotalRecords,
            dto.Processed,
            dto.Matched,
            dto.Duplicate,
            dto.Rejected,
            dto.Ignored,
            dto.Failed,
            DateTime.UtcNow);

        var updated = await runRepository.UpdateAsync(run).ConfigureAwait(false);
        return MapToDto(updated);
    }

    public async Task<CassoReconciliationRunDto> FailAsync(FailCassoReconciliationRunDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var run = await GetRunAsync(dto.RunId).ConfigureAwait(false);
        run.Fail(dto.ErrorMessage, DateTime.UtcNow);
        var updated = await runRepository.UpdateAsync(run).ConfigureAwait(false);
        return MapToDto(updated);
    }

    private async Task<CassoReconciliationRun> GetRunAsync(Guid id)
        => await runRepository.GetByIdAsync(id).ConfigureAwait(false)
            ?? throw new NamEcommerceDomainException("Error.CassoReconciliationRunIsNotFound");

    private static CassoReconciliationRunDto MapToDto(CassoReconciliationRun run)
        => new(run.Id)
        {
            StartedAtUtc = run.StartedAtUtc,
            FinishedAtUtc = run.FinishedAtUtc,
            FromDate = run.FromDate,
            ToDate = run.ToDate,
            Trigger = run.Trigger,
            TotalRecords = run.TotalRecords,
            Processed = run.Processed,
            Matched = run.Matched,
            Duplicate = run.Duplicate,
            Rejected = run.Rejected,
            Ignored = run.Ignored,
            Failed = run.Failed,
            ErrorMessage = run.ErrorMessage
        };
}
