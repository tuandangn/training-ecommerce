using NamEcommerce.Web.Contracts.Models.Common;

namespace NamEcommerce.Web.Contracts.Commands.Models.Finance;

public sealed class CreateBankAccountCommand : ICommand<CommonActionResultModel>
{
    public required string DisplayName { get; init; }
    public required string BankCode { get; init; }
    public required string BankName { get; init; }
    public required string AccountNumber { get; init; }
    public required string AccountHolderName { get; init; }
    public decimal OpeningBalance { get; init; }
    public bool SetAsDefault { get; init; }
}

public sealed class UpdateBankAccountCommand : ICommand<CommonActionResultModel>
{
    public required Guid Id { get; init; }
    public required string DisplayName { get; init; }
    public required string BankCode { get; init; }
    public required string BankName { get; init; }
    public required string AccountNumber { get; init; }
    public required string AccountHolderName { get; init; }
}

public sealed record SetDefaultBankAccountCommand(Guid Id) : ICommand<CommonActionResultModel>;
public sealed record DeactivateBankAccountCommand(Guid Id) : ICommand<CommonActionResultModel>;
public sealed record ActivateBankAccountCommand(Guid Id) : ICommand<CommonActionResultModel>;
