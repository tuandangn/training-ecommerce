using NamEcommerce.Domain.Shared.Dtos.Finance;

namespace NamEcommerce.Domain.Shared.Services.Finance;

public interface IExpenseManager
{
    Task<CreateExpenseResultDto> CreateExpenseAsync(CreateExpenseDto dto);
    Task UpdateExpenseAsync(UpdateExpenseDto dto);
    Task DeleteExpenseAsync(Guid id);
}
