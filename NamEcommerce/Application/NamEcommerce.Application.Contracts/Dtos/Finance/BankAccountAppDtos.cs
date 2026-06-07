namespace NamEcommerce.Application.Contracts.Dtos.Finance;

public sealed class BankAccountAppDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string BankCode { get; set; } = string.Empty;
    public string BankName { get; set; } = string.Empty;
    public string AccountNumber { get; set; } = string.Empty;
    public string AccountHolderName { get; set; } = string.Empty;
    public decimal OpeningBalance { get; set; }
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; }
    public decimal CurrentBalance { get; set; }   // computed khi query, set bởi AppService/ModelFactory
}

public sealed class CreateBankAccountAppDto
{
    public string DisplayName { get; set; } = string.Empty;
    public string BankCode { get; set; } = string.Empty;
    public string BankName { get; set; } = string.Empty;
    public string AccountNumber { get; set; } = string.Empty;
    public string AccountHolderName { get; set; } = string.Empty;
    public decimal OpeningBalance { get; set; }
    public bool SetAsDefault { get; set; }

    public (bool valid, string? error) Validate()
    {
        if (string.IsNullOrWhiteSpace(DisplayName)) return (false, "Error.BankAccount.DisplayNameRequired");
        if (string.IsNullOrWhiteSpace(BankCode)) return (false, "Error.BankAccount.BankCodeRequired");
        if (string.IsNullOrWhiteSpace(BankName)) return (false, "Error.BankAccount.BankNameRequired");
        if (string.IsNullOrWhiteSpace(AccountNumber)) return (false, "Error.BankAccount.AccountNumberRequired");
        if (string.IsNullOrWhiteSpace(AccountHolderName)) return (false, "Error.BankAccount.AccountHolderNameRequired");
        if (OpeningBalance < 0) return (false, "Error.BankAccount.OpeningBalanceCannotBeNegative");
        return (true, null);
    }
}

public sealed class UpdateBankAccountAppDto
{
    public Guid Id { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string BankCode { get; set; } = string.Empty;
    public string BankName { get; set; } = string.Empty;
    public string AccountNumber { get; set; } = string.Empty;
    public string AccountHolderName { get; set; } = string.Empty;
}

public sealed class BankAccountOperationResultAppDto
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public Guid? AccountId { get; set; }
}
