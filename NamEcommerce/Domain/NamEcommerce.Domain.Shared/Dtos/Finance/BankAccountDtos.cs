using NamEcommerce.Domain.Shared.Exceptions.Finance;

namespace NamEcommerce.Domain.Shared.Dtos.Finance;

public sealed class BankAccountDto
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
}

public sealed class CreateBankAccountDto
{
    public string DisplayName { get; set; } = string.Empty;
    public string BankCode { get; set; } = string.Empty;
    public string BankName { get; set; } = string.Empty;
    public string AccountNumber { get; set; } = string.Empty;
    public string AccountHolderName { get; set; } = string.Empty;
    public decimal OpeningBalance { get; set; }
    public bool SetAsDefault { get; set; }

    public void Verify()
    {
        if (string.IsNullOrWhiteSpace(DisplayName))
            throw new BankAccountDataInvalidException("Error.BankAccount.DisplayNameRequired");
        if (string.IsNullOrWhiteSpace(BankCode))
            throw new BankAccountDataInvalidException("Error.BankAccount.BankCodeRequired");
        if (string.IsNullOrWhiteSpace(BankName))
            throw new BankAccountDataInvalidException("Error.BankAccount.BankNameRequired");
        if (string.IsNullOrWhiteSpace(AccountNumber))
            throw new BankAccountDataInvalidException("Error.BankAccount.AccountNumberRequired");
        if (string.IsNullOrWhiteSpace(AccountHolderName))
            throw new BankAccountDataInvalidException("Error.BankAccount.AccountHolderNameRequired");
        if (OpeningBalance < 0)
            throw new BankAccountDataInvalidException("Error.BankAccount.OpeningBalanceCannotBeNegative");
    }
}

public sealed class UpdateBankAccountDto
{
    public Guid Id { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string BankCode { get; set; } = string.Empty;
    public string BankName { get; set; } = string.Empty;
    public string AccountNumber { get; set; } = string.Empty;
    public string AccountHolderName { get; set; } = string.Empty;
}

public sealed class CreateBankAccountResultDto
{
    public Guid CreatedId { get; set; }
}
