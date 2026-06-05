using NamEcommerce.Application.Contracts.Debts;
using NamEcommerce.Application.Contracts.Dtos.Debts;
using NamEcommerce.Domain.Shared.Dtos.Debts;
using NamEcommerce.Domain.Shared.Enums.Debts;
using NamEcommerce.Domain.Shared.Services.Debts;
using NamEcommerce.Domain.Shared.Settings;

namespace NamEcommerce.Application.Services.Debts;

public sealed class CassoBankTransferAppService(
    CassoTransactionMapper mapper,
    ICassoTransactionClient transactionClient,
    IBankTransferPaymentIntentAppService paymentIntentAppService,
    ICassoReconciliationRunManager reconciliationRunManager,
    BankTransferPaymentSettings settings) : ICassoBankTransferAppService
{
    public async Task<CassoBankTransferProcessingResultAppDto> ProcessWebhookAsync(ProcessCassoWebhookAppDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        if (!IsCassoEnabled())
            return Error("Error.CassoVerificationDisabled");
        if (!settings.Casso.WebhookEnabled)
            return Error("Error.CassoWebhookDisabled");
        if (dto.Data.Count == 0)
            return Success([]);

        return await ProcessTransactionsAsync(dto.Data, BankTransferVerificationSource.BankWebhook).ConfigureAwait(false);
    }

    public async Task<CassoBankTransferProcessingResultAppDto> RunReconciliationAsync(RunCassoReconciliationAppDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        if (!IsCassoEnabled())
            return Error("Error.CassoVerificationDisabled");
        if (!settings.Casso.ReconciliationEnabled)
            return Error("Error.CassoReconciliationDisabled");

        var (fromDate, toDate) = ResolveDateRange(dto);
        var run = await reconciliationRunManager.StartAsync(new StartCassoReconciliationRunDto
        {
            FromDate = fromDate,
            ToDate = toDate,
            Trigger = (CassoReconciliationRunTrigger)dto.Trigger
        }).ConfigureAwait(false);

        try
        {
            var allResults = new List<CassoTransactionProcessingResultAppDto>();
            var page = 1;
            var pageSize = Math.Max(1, settings.Casso.ReconciliationPageSize);
            var hasMore = true;

            while (hasMore)
            {
                var cassoPage = await transactionClient.GetTransactionsAsync(new GetCassoTransactionsAppDto
                {
                    FromDate = fromDate,
                    ToDate = toDate,
                    Page = page,
                    PageSize = pageSize
                }, CancellationToken.None).ConfigureAwait(false);

                var pageResult = await ProcessTransactionsAsync(cassoPage.Records, BankTransferVerificationSource.BankStatement)
                    .ConfigureAwait(false);
                allResults.AddRange(pageResult.Results);

                hasMore = cassoPage.HasMore;
                page += 1;
            }

            var result = Success(allResults) with { RunId = run.Id };
            await CompleteRunAsync(run.Id, result).ConfigureAwait(false);
            return result;
        }
        catch (Exception ex)
        {
            await reconciliationRunManager.FailAsync(new FailCassoReconciliationRunDto
            {
                RunId = run.Id,
                ErrorMessage = ex.Message
            }).ConfigureAwait(false);

            return Error(ex.Message) with { RunId = run.Id };
        }
    }

    private async Task<CassoBankTransferProcessingResultAppDto> ProcessTransactionsAsync(
        IEnumerable<CassoTransactionAppDto> transactions,
        BankTransferVerificationSource source)
    {
        var results = new List<CassoTransactionProcessingResultAppDto>();

        foreach (var transaction in transactions)
        {
            var mapped = mapper.Map(transaction, source);
            if (!mapped.CanProcess || mapped.ProviderTransaction is null)
            {
                results.Add(new CassoTransactionProcessingResultAppDto
                {
                    Success = true,
                    Ignored = true,
                    Message = mapped.IgnoreReason,
                    ProviderTransactionId = transaction.Id?.ToString()
                });
                continue;
            }

            var processResult = await paymentIntentAppService
                .ProcessProviderTransactionAsync(mapped.ProviderTransaction)
                .ConfigureAwait(false);

            results.Add(new CassoTransactionProcessingResultAppDto
            {
                Success = processResult.Success,
                Ignored = false,
                Message = processResult.ErrorMessage,
                ProviderTransactionId = mapped.ProviderTransaction.ProviderTransactionId,
                VerificationLogId = processResult.VerificationLogId
            });
        }

        return Success(results);
    }

    private async Task CompleteRunAsync(Guid runId, CassoBankTransferProcessingResultAppDto result)
    {
        await reconciliationRunManager.CompleteAsync(new CompleteCassoReconciliationRunDto
        {
            RunId = runId,
            TotalRecords = result.TotalRecords,
            Processed = result.Processed,
            Matched = result.Matched,
            Duplicate = result.Duplicate,
            Rejected = result.Rejected,
            Ignored = result.Ignored,
            Failed = result.Failed
        }).ConfigureAwait(false);
    }

    private bool IsCassoEnabled()
        => settings.Casso.Enabled
           && string.Equals(settings.Verification.Provider, "Casso", StringComparison.OrdinalIgnoreCase);

    private (DateTime fromDate, DateTime toDate) ResolveDateRange(RunCassoReconciliationAppDto dto)
    {
        var toDate = dto.ToDate?.Date ?? DateTime.Today;
        var fromDate = dto.FromDate?.Date ?? DateTime.UtcNow.AddMinutes(-settings.Casso.ReconciliationLookbackMinutes).Date;
        return (fromDate, toDate);
    }

    private static CassoBankTransferProcessingResultAppDto Success(IList<CassoTransactionProcessingResultAppDto> results)
        => new()
        {
            Success = true,
            TotalRecords = results.Count,
            Processed = results.Count(result => !result.Ignored),
            Matched = results.Count(result => result.Success && !result.Ignored),
            Duplicate = results.Count(result => result.Message == "Error.PaymentIntentProviderTransactionDuplicated"),
            Rejected = results.Count(result => !result.Success
                                               && !result.Ignored
                                               && result.Message != "Error.PaymentIntentProviderTransactionDuplicated"),
            Ignored = results.Count(result => result.Ignored),
            Failed = 0,
            Results = results
        };

    private static CassoBankTransferProcessingResultAppDto Error(string errorMessage)
        => new()
        {
            Success = false,
            ErrorMessage = errorMessage,
            Failed = 1
        };
}
