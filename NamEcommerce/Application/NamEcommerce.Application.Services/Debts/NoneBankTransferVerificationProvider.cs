using NamEcommerce.Application.Contracts.Debts;
using NamEcommerce.Application.Contracts.Dtos.Debts;

namespace NamEcommerce.Application.Services.Debts;

public sealed class NoneBankTransferVerificationProvider : IBankTransferVerificationProvider
{
    public Task<BankTransferVerificationProviderResultAppDto> VerifyAsync(BankTransferVerificationRequestAppDto dto)
        => Task.FromResult(new BankTransferVerificationProviderResultAppDto
        {
            Success = false,
            ErrorMessage = "Error.BankTransferVerificationProviderNotConfigured"
        });
}
