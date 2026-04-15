using NamEcommerce.Application.Contracts.Dtos.Common;
using NamEcommerce.Application.Contracts.Dtos.Preparation;

namespace NamEcommerce.Application.Contracts.Preparation;

public interface IPreparationAppService
{
    Task<IPagedDataAppDto<PreparationItemAppDto>> GetPreparationListAsync(int pageIndex, int pageSize, string? keywords = null);
    Task<IPagedDataAppDto<PreparationGroupedItemAppDto>> GetPreparationGroupedListAsync(int pageIndex, int pageSize, string? keywords = null);
}
