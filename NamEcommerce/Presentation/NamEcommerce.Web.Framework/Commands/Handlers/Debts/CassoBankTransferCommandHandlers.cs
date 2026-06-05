using MediatR;
using NamEcommerce.Application.Contracts.Debts;
using NamEcommerce.Application.Contracts.Dtos.Debts;
using NamEcommerce.Web.Contracts.Commands.Models.Debts;
using NamEcommerce.Web.Contracts.Models.Debts;

namespace NamEcommerce.Web.Framework.Commands.Handlers.Debts;

public sealed class ProcessCassoWebhookCommandHandler(ICassoBankTransferAppService cassoAppService)
    : IRequestHandler<ProcessCassoWebhookCommand, CassoBankTransferProcessingResultModel>
{
    public async Task<CassoBankTransferProcessingResultModel> Handle(ProcessCassoWebhookCommand request, CancellationToken cancellationToken)
    {
        var result = await cassoAppService.ProcessWebhookAsync(new ProcessCassoWebhookAppDto
        {
            Error = request.Error,
            Data = request.Data.Select(CassoBankTransferCommandHandlerMapper.MapTransaction).ToList(),
            RawPayload = request.RawPayload
        }).ConfigureAwait(false);

        return CassoBankTransferCommandHandlerMapper.Map(result);
    }
}

public sealed class RunCassoReconciliationCommandHandler(ICassoBankTransferAppService cassoAppService)
    : IRequestHandler<RunCassoReconciliationCommand, CassoBankTransferProcessingResultModel>
{
    public async Task<CassoBankTransferProcessingResultModel> Handle(RunCassoReconciliationCommand request, CancellationToken cancellationToken)
    {
        var result = await cassoAppService.RunReconciliationAsync(new RunCassoReconciliationAppDto
        {
            FromDate = request.FromDate,
            ToDate = request.ToDate,
            Trigger = request.Trigger
        }).ConfigureAwait(false);

        return CassoBankTransferCommandHandlerMapper.Map(result);
    }
}

internal static class CassoBankTransferCommandHandlerMapper
{
    public static CassoBankTransferProcessingResultModel Map(CassoBankTransferProcessingResultAppDto result)
        => new()
        {
            Success = result.Success,
            ErrorMessage = result.ErrorMessage,
            RunId = result.RunId,
            TotalRecords = result.TotalRecords,
            Processed = result.Processed,
            Matched = result.Matched,
            Duplicate = result.Duplicate,
            Rejected = result.Rejected,
            Ignored = result.Ignored,
            Failed = result.Failed,
            Results = result.Results.Select(Map).ToList()
        };

    public static CassoTransactionAppDto MapTransaction(CassoTransactionCommand command)
        => new()
        {
            Id = command.Id,
            Tid = command.Tid,
            Description = command.Description,
            Amount = command.Amount,
            When = command.When,
            BankSubAccId = command.BankSubAccId,
            SubAccId = command.SubAccId,
            BankSubAccIdCamel = command.BankSubAccIdCamel,
            BankName = command.BankName,
            BankAbbreviation = command.BankAbbreviation,
            BankCodeName = command.BankCodeName
        };

    private static CassoTransactionProcessingResultModel Map(CassoTransactionProcessingResultAppDto result)
        => new()
        {
            Success = result.Success,
            Ignored = result.Ignored,
            Message = result.Message,
            ProviderTransactionId = result.ProviderTransactionId,
            VerificationLogId = result.VerificationLogId
        };
}
