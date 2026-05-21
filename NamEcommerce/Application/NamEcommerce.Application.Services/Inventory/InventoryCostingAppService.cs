using NamEcommerce.Application.Contracts.Dtos.Inventory;
using NamEcommerce.Application.Contracts.Inventory;
using NamEcommerce.Data.Contracts;
using NamEcommerce.Domain.Entities.Inventory;
using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Domain.Shared.Enums.Inventory;
using NamEcommerce.Domain.Shared.Exceptions.Inventory;
using NamEcommerce.Domain.Shared.Services.Inventory;
using NamEcommerce.Domain.Shared.Services.Users;

namespace NamEcommerce.Application.Services.Inventory;

public sealed class InventoryCostingAppService : IInventoryCostingAppService
{
    private readonly IRepository<InventoryCostingPolicy> _policyRepository;
    private readonly IEntityDataReader<InventoryCostingPolicy> _policyReader;
    private readonly IInventoryCostingManager _inventoryCostingManager;
    private readonly ICurrentUserAccessor _currentUserAccessor;

    public InventoryCostingAppService(
        IRepository<InventoryCostingPolicy> policyRepository,
        IEntityDataReader<InventoryCostingPolicy> policyReader,
        IInventoryCostingManager inventoryCostingManager,
        ICurrentUserAccessor currentUserAccessor)
    {
        _policyRepository = policyRepository;
        _policyReader = policyReader;
        _inventoryCostingManager = inventoryCostingManager;
        _currentUserAccessor = currentUserAccessor;
    }

    public async Task<InventoryCostingPolicyAppDto> GetActivePolicyAsync()
    {
        var policy = await EnsureActivePolicyAsync().ConfigureAwait(false);
        return Map(policy);
    }

    public async Task<InventoryCostingPolicyAppDto> UpdatePolicyAsync(UpdateInventoryCostingPolicyAppDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        var costingMethod = (InventoryCostingMethod)dto.CostingMethod;
        var valuationScope = (InventoryValuationScope)dto.ValuationScope;
        EnsureSupportedPolicy(costingMethod, valuationScope);

        var activePolicies = _policyReader.DataSource
            .Where(p => p.IsActive)
            .ToList();

        foreach (var activePolicy in activePolicies)
        {
            activePolicy.Deactivate();
            await _policyRepository.UpdateAsync(activePolicy).ConfigureAwait(false);
        }

        var currentUser = await _currentUserAccessor.GetCurrentUserAsync().ConfigureAwait(false);
        var policy = new InventoryCostingPolicy(
            Guid.NewGuid(),
            costingMethod,
            valuationScope,
            dto.EffectiveFromUtc,
            currentUser?.Id,
            dto.Note);

        policy = await _policyRepository.InsertAsync(policy).ConfigureAwait(false);
        return Map(policy);
    }

    public async Task<Guid> RebuildAllAsync(RebuildInventoryCostingAppDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        var costingMethod = (InventoryCostingMethod)dto.CostingMethod;
        var valuationScope = (InventoryValuationScope)dto.ValuationScope;
        EnsureSupportedPolicy(costingMethod, valuationScope);

        var currentUser = await _currentUserAccessor.GetCurrentUserAsync().ConfigureAwait(false);
        return await _inventoryCostingManager.RebuildAllAsync(
            costingMethod,
            valuationScope,
            currentUser?.Id).ConfigureAwait(false);
    }

    private async Task<InventoryCostingPolicy> EnsureActivePolicyAsync()
    {
        var policy = _policyReader.DataSource
            .Where(p => p.IsActive)
            .OrderByDescending(p => p.EffectiveFromUtc)
            .ThenByDescending(p => p.CreatedAtUtc)
            .FirstOrDefault();

        if (policy is not null)
            return policy;

        var defaultPolicy = new InventoryCostingPolicy(
            Guid.NewGuid(),
            InventoryCostingMethod.WeightedAverage,
            InventoryValuationScope.Product,
            DateTime.UtcNow,
            null,
            "Default inventory costing policy");

        return await _policyRepository.InsertAsync(defaultPolicy).ConfigureAwait(false);
    }

    private static InventoryCostingPolicyAppDto Map(InventoryCostingPolicy policy)
        => new()
        {
            Id = policy.Id,
            CostingMethod = (int)policy.CostingMethod,
            ValuationScope = (int)policy.ValuationScope,
            EffectiveFromUtc = policy.EffectiveFromUtc,
            IsActive = policy.IsActive,
            CreatedByUserId = policy.CreatedByUserId,
            CreatedAtUtc = policy.CreatedAtUtc,
            Note = policy.Note
        };

    private static void EnsureSupportedPolicy(InventoryCostingMethod method, InventoryValuationScope scope)
    {
        if (method != InventoryCostingMethod.WeightedAverage)
            throw new UnsupportedInventoryCostingMethodException(method);

        if (scope != InventoryValuationScope.Product)
            throw new InvalidInventoryCostingOperationException("Error.InventoryCosting.ProductWarehouseScopeNotSupported");
    }
}
