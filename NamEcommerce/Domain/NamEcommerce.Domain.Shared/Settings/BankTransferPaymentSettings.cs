namespace NamEcommerce.Domain.Shared.Settings;

[Serializable]
public sealed class BankTransferPaymentSettings
{
    public const string SectionName = "Payments:BankTransfer";

    public bool Enabled { get; init; }
    public string BankId { get; init; } = string.Empty;
    public string AccountNo { get; init; } = string.Empty;
    public string AccountName { get; init; } = string.Empty;
    public string Template { get; init; } = "compact2";
    public string TransferContentPrefix { get; init; } = "QS";
    public int IntentExpiryMinutes { get; init; } = 15;
    public int StatusPollingSeconds { get; init; } = 3;
    public BankTransferWebhookSettings Webhook { get; init; } = new();
    public BankTransferVerificationSettings Verification { get; init; } = new();
    public CassoPaymentSettings Casso { get; init; } = new();
}

[Serializable]
public sealed class BankTransferWebhookSettings
{
    public bool Enabled { get; init; }
    public string SecretToken { get; init; } = string.Empty;
}

[Serializable]
public sealed class BankTransferVerificationSettings
{
    public string Provider { get; init; } = "None";
    public bool AllowManualConfirm { get; init; } = true;
}

[Serializable]
public sealed class CassoPaymentSettings
{
    public bool Enabled { get; init; }
    public string ApiBaseUrl { get; init; } = "https://oauth.casso.vn";
    public string ApiKey { get; init; } = string.Empty;
    public bool WebhookEnabled { get; init; }
    public string WebhookSecurityHeaderName { get; init; } = "X-NamEcommerce-Casso-Token";
    public string WebhookSecurityKey { get; init; } = string.Empty;
    public bool ReconciliationEnabled { get; init; }
    public int ReconciliationIntervalMinutes { get; init; } = 15;
    public int ReconciliationLookbackMinutes { get; init; } = 180;
    public int ReconciliationPageSize { get; init; } = 50;
}
