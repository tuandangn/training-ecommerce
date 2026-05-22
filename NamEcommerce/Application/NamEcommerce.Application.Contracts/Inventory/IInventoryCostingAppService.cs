using NamEcommerce.Application.Contracts.Dtos.Inventory;

namespace NamEcommerce.Application.Contracts.Inventory;

public interface IInventoryCostingAppService
{
    Task<InventoryCostingPolicyAppDto> GetActivePolicyAsync();
    Task<InventoryCostingPolicyAppDto> UpdatePolicyAsync(UpdateInventoryCostingPolicyAppDto dto);
    Task<Guid> RebuildAllAsync(RebuildInventoryCostingAppDto dto);
}
