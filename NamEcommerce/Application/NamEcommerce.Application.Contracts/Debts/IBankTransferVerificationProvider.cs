using NamEcommerce.Application.Contracts.Dtos.Debts;

namespace NamEcommerce.Application.Contracts.Debts;

public interface IBankTransferVerificationProvider
{
    Task<BankTransferVerificationProviderResultAppDto> VerifyAsync(BankTransferVerificationRequestAppDto dto);
}
