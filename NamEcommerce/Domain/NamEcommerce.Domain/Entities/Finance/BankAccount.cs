using NamEcommerce.Domain.Shared;
using NamEcommerce.Domain.Shared.Exceptions.Finance;

namespace NamEcommerce.Domain.Entities.Finance;

[Serializable]
public sealed record BankAccount : AppAggregateEntity
{
    private BankAccount() : base(Guid.Empty) { }

    internal BankAccount(
        string code,
        string displayName,
        string bankCode,
        string bankName,
        string accountNumber,
        string accountHolderName,
        decimal openingBalance) : base(Guid.NewGuid())
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        ArgumentException.ThrowIfNullOrWhiteSpace(bankCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(bankName);
        ArgumentException.ThrowIfNullOrWhiteSpace(accountNumber);
        ArgumentException.ThrowIfNullOrWhiteSpace(accountHolderName);
        if (openingBalance < 0)
            throw new BankAccountDataInvalidException("Error.BankAccount.OpeningBalanceCannotBeNegative");

        Code = code;
        DisplayName = displayName;
        BankCode = bankCode;
        BankName = bankName;
        AccountNumber = accountNumber;
        AccountHolderName = accountHolderName;
        OpeningBalance = openingBalance;
        IsDefault = false;
        IsActive = true;
        CreatedOnUtc = DateTime.UtcNow;
    }

    public string Code { get; private set; } = string.Empty;
    public string DisplayName { get; private set; } = string.Empty;
    public string BankCode { get; private set; } = string.Empty;
    public string BankName { get; private set; } = string.Empty;
    public string AccountNumber { get; private set; } = string.Empty;
    public string AccountHolderName { get; private set; } = string.Empty;
    public decimal OpeningBalance { get; private set; }
    public bool IsDefault { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedOnUtc { get; private set; }
    public DateTime? UpdatedOnUtc { get; private set; }

    internal void UpdateInfo(string displayName, string bankCode, string bankName, string accountNumber, string accountHolderName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        ArgumentException.ThrowIfNullOrWhiteSpace(bankCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(bankName);
        ArgumentException.ThrowIfNullOrWhiteSpace(accountNumber);
        ArgumentException.ThrowIfNullOrWhiteSpace(accountHolderName);
        DisplayName = displayName;
        BankCode = bankCode;
        BankName = bankName;
        AccountNumber = accountNumber;
        AccountHolderName = accountHolderName;
        UpdatedOnUtc = DateTime.UtcNow;
    }

    internal void SetAsDefault()
    {
        if (!IsActive)
            throw new BankAccountDataInvalidException("Error.BankAccount.CannotSetInactiveAsDefault");
        IsDefault = true;
        UpdatedOnUtc = DateTime.UtcNow;
    }

    internal void ClearDefault()
    {
        IsDefault = false;
        UpdatedOnUtc = DateTime.UtcNow;
    }

    internal void Deactivate()
    {
        if (IsDefault)
            throw new BankAccountIsDefaultException();
        IsActive = false;
        UpdatedOnUtc = DateTime.UtcNow;
    }

    internal void Activate()
    {
        IsActive = true;
        UpdatedOnUtc = DateTime.UtcNow;
    }
}
