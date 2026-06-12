using System.Text.Json.Serialization;
using NamEcommerce.Web.Contracts.Models.Debts;

namespace NamEcommerce.Web.Contracts.Commands.Models.Debts;

[Serializable]
public sealed record CassoTransactionCommand
{
    [JsonPropertyName("id")]
    public long? Id { get; init; }

    [JsonPropertyName("tid")]
    public string? Tid { get; init; }

    [JsonPropertyName("description")]
    public string? Description { get; init; }

    [JsonPropertyName("amount")]
    public decimal Amount { get; init; }

    [JsonPropertyName("when")]
    public string? When { get; init; }

    [JsonPropertyName("bank_sub_acc_id")]
    public string? BankSubAccId { get; init; }

    [JsonPropertyName("subAccId")]
    public string? SubAccId { get; init; }

    [JsonPropertyName("bankSubAccId")]
    public string? BankSubAccIdCamel { get; init; }

    [JsonPropertyName("bankName")]
    public string? BankName { get; init; }

    [JsonPropertyName("bankAbbreviation")]
    public string? BankAbbreviation { get; init; }

    [JsonPropertyName("bankCodeName")]
    public string? BankCodeName { get; init; }
}

[Serializable]
public sealed record ProcessCassoWebhookCommand : ICommand<CassoBankTransferProcessingResultModel>
{
    public int? Error { get; init; }
    public IList<CassoTransactionCommand> Data { get; init; } = [];
    public string RawPayload { get; init; } = string.Empty;
}

[Serializable]
public sealed record RunCassoReconciliationCommand : ICommand<CassoBankTransferProcessingResultModel>
{
    public DateTime? FromDate { get; init; }
    public DateTime? ToDate { get; init; }
    public int Trigger { get; init; } = 10;
}
