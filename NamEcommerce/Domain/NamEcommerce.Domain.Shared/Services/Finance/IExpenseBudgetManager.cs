using NamEcommerce.Domain.Shared.Dtos.Finance;

namespace NamEcommerce.Domain.Shared.Services.Finance;

public interface IExpenseBudgetManager
{
    Task UpsertAsync(UpsertExpenseBudgetDto dto);
}
