using NamEcommerce.Application.Contracts.Dtos.Debts;

namespace NamEcommerce.Application.Contracts.Debts;

public interface IBankTransferReceivingAccountResolver
{
    Task<BankTransferReceivingAccountAppDto?> ResolveAsync();
}
