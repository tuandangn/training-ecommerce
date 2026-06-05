using NamEcommerce.Application.Contracts.Dtos.Debts;

namespace NamEcommerce.Application.Contracts.Debts;

public interface ICassoTransactionClient
{
    Task<CassoTransactionPageAppDto> GetTransactionsAsync(GetCassoTransactionsAppDto dto, CancellationToken cancellationToken);
}
