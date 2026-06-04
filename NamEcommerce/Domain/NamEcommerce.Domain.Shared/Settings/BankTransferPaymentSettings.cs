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
    public BankTransferVerificationSettings Verification { get; init; } = new();
}

[Serializable]
public sealed class BankTransferVerificationSettings
{
    public string Provider { get; init; } = "None";
    public bool AllowManualConfirm { get; init; } = true;
}
