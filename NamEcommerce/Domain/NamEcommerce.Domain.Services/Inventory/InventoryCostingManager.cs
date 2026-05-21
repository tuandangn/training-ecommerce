using NamEcommerce.Data.Contracts;
using NamEcommerce.Domain.Entities.Inventory;
using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Domain.Shared.Dtos.Inventory;
using NamEcommerce.Domain.Shared.Enums.Inventory;
using NamEcommerce.Domain.Shared.Exceptions.Inventory;
using NamEcommerce.Domain.Shared.Services.Inventory;

namespace NamEcommerce.Domain.Services.Inventory;

public sealed class InventoryCostingManager : IInventoryCostingManager
{
    private readonly IRepository<InventoryCostingPolicy> _policyRepository;
    private readonly IRepository<InventoryCostLedgerEntry> _ledgerRepository;
    private readonly IRepository<InventoryCostLayer> _layerRepository;
    private readonly IRepository<InventoryCostAllocation> _allocationRepository;
    private readonly IRepository<InventoryCostRebuildRun> _rebuildRunRepository;
    private readonly IEntityDataReader<InventoryCostingPolicy> _policyReader;
    private readonly IEntityDataReader<InventoryCostLedgerEntry> _ledgerReader;
    private readonly IEntityDataReader<InventoryCostLayer> _layerReader;
    private readonly IEntityDataReader<InventoryCostAllocation> _allocationReader;

    public InventoryCostingManager(
        IRepository<InventoryCostingPolicy> policyRepository,
        IRepository<InventoryCostLedgerEntry> ledgerRepository,
        IRepository<InventoryCostLayer> layerRepository,
        IRepository<InventoryCostAllocation> allocationRepository,
        IRepository<InventoryCostRebuildRun> rebuildRunRepository,
        IEntityDataReader<InventoryCostingPolicy> policyReader,
        IEntityDataReader<InventoryCostLedgerEntry> ledgerReader,
        IEntityDataReader<InventoryCostLayer> layerReader,
        IEntityDataReader<InventoryCostAllocation> allocationReader)
    {
        _policyRepository = policyRepository;
        _ledgerRepository = ledgerRepository;
        _layerRepository = layerRepository;
        _allocationRepository = allocationRepository;
        _rebuildRunRepository = rebuildRunRepository;
        _policyReader = policyReader;
        _ledgerReader = ledgerReader;
        _layerReader = layerReader;
        _allocationReader = allocationReader;
    }

    public async Task<InventoryCostMovementResultDto> RegisterInboundAsync(RegisterInventoryInboundCostDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        ValidatePositiveQuantity(dto.Quantity);

        var policy = await GetActivePolicyAsync().ConfigureAwait(false);
        EnsureWeightedAverage(policy);

        var occurredAtUtc = dto.OccurredAtUtc ?? DateTime.UtcNow;
        var balance = GetLastProductBalance(dto.ProductId);
        var sequenceNumber = GetNextSequenceNumber();
        var hasPendingCost = !dto.UnitCost.HasValue || HasOpenPendingLayer(dto.ProductId, occurredAtUtc);

        var inboundValue = dto.UnitCost.HasValue ? dto.Quantity * dto.UnitCost.Value : 0m;
        var quantityBalance = balance.Quantity + dto.Quantity;
        var valueBalance = dto.UnitCost.HasValue ? balance.Value + inboundValue : balance.Value;
        var averageCost = quantityBalance > 0 ? valueBalance / quantityBalance : 0m;
        var status = hasPendingCost ? InventoryCostingStatus.Pending : InventoryCostingStatus.Final;

        var ledgerEntry = new InventoryCostLedgerEntry(
            Guid.NewGuid(),
            dto.ProductId,
            dto.WarehouseId,
            occurredAtUtc,
            sequenceNumber,
            dto.MovementType,
            dto.Quantity,
            dto.UnitCost,
            dto.UnitCost.HasValue ? inboundValue : null,
            quantityBalance,
            valueBalance,
            averageCost,
            status,
            policy.CostingMethod,
            policy.ValuationScope,
            dto.ReferenceType,
            dto.ReferenceId,
            dto.ReferenceItemId,
            null);

        await _ledgerRepository.InsertAsync(ledgerEntry).ConfigureAwait(false);

        var layer = new InventoryCostLayer(
            Guid.NewGuid(),
            dto.ProductId,
            dto.WarehouseId,
            ledgerEntry.Id,
            dto.ReferenceType,
            dto.ReferenceId,
            dto.ReferenceItemId,
            occurredAtUtc,
            dto.Quantity,
            dto.Quantity,
            dto.UnitCost,
            dto.UnitCost.HasValue ? inboundValue : null,
            status,
            policy.CostingMethod,
            policy.ValuationScope,
            null);

        await _layerRepository.InsertAsync(layer).ConfigureAwait(false);

        return new InventoryCostMovementResultDto
        {
            LedgerEntryId = ledgerEntry.Id,
            UnitCost = dto.UnitCost ?? averageCost,
            TotalCost = dto.UnitCost.HasValue ? inboundValue : 0m,
            Status = status
        };
    }

    public async Task<InventoryCostMovementResultDto> RegisterOutboundAsync(RegisterInventoryOutboundCostDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        ValidatePositiveQuantity(dto.Quantity);

        var policy = await GetActivePolicyAsync().ConfigureAwait(false);
        EnsureWeightedAverage(policy);

        var occurredAtUtc = dto.OccurredAtUtc ?? DateTime.UtcNow;
        var balance = GetLastProductBalance(dto.ProductId);
        var sequenceNumber = GetNextSequenceNumber();
        var unitCost = balance.AverageCost;
        var totalCost = dto.Quantity * unitCost;
        var hasPendingCost = HasOpenPendingLayer(dto.ProductId, occurredAtUtc);
        var status = hasPendingCost ? InventoryCostingStatus.Pending : InventoryCostingStatus.Final;
        var quantityBalance = balance.Quantity - dto.Quantity;
        var valueBalance = balance.Value - totalCost;
        if (quantityBalance <= 0)
        {
            quantityBalance = 0;
            valueBalance = 0;
        }

        var averageCost = quantityBalance > 0 ? valueBalance / quantityBalance : 0m;

        var ledgerEntry = new InventoryCostLedgerEntry(
            Guid.NewGuid(),
            dto.ProductId,
            dto.WarehouseId,
            occurredAtUtc,
            sequenceNumber,
            dto.MovementType,
            -dto.Quantity,
            unitCost,
            totalCost,
            quantityBalance,
            valueBalance,
            averageCost,
            status,
            policy.CostingMethod,
            policy.ValuationScope,
            dto.ReferenceType,
            dto.ReferenceId,
            dto.ReferenceItemId,
            null);

        await _ledgerRepository.InsertAsync(ledgerEntry).ConfigureAwait(false);

        var allocation = new InventoryCostAllocation(
            Guid.NewGuid(),
            dto.ProductId,
            dto.WarehouseId,
            ledgerEntry.Id,
            dto.ReferenceType,
            dto.ReferenceId,
            dto.ReferenceItemId,
            null,
            dto.Quantity,
            unitCost,
            totalCost,
            status,
            policy.CostingMethod,
            policy.ValuationScope,
            null);

        await _allocationRepository.InsertAsync(allocation).ConfigureAwait(false);

        return new InventoryCostMovementResultDto
        {
            LedgerEntryId = ledgerEntry.Id,
            UnitCost = unitCost,
            TotalCost = totalCost,
            Status = status
        };
    }

    public async Task<InventoryCostMovementResultDto> RegisterTransferInAsync(RegisterInventoryTransferInCostDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        ValidatePositiveQuantity(dto.Quantity);

        var policy = await GetActivePolicyAsync().ConfigureAwait(false);
        EnsureWeightedAverage(policy);

        var occurredAtUtc = dto.OccurredAtUtc ?? DateTime.UtcNow;
        var balance = GetLastProductBalance(dto.ProductId);
        var sequenceNumber = GetNextSequenceNumber();
        var inboundValue = dto.Quantity * dto.UnitCost;
        var quantityBalance = balance.Quantity + dto.Quantity;
        var valueBalance = balance.Value + inboundValue;
        var averageCost = quantityBalance > 0 ? valueBalance / quantityBalance : 0m;

        var ledgerEntry = new InventoryCostLedgerEntry(
            Guid.NewGuid(),
            dto.ProductId,
            dto.WarehouseId,
            occurredAtUtc,
            sequenceNumber,
            InventoryCostMovementType.TransferIn,
            dto.Quantity,
            dto.UnitCost,
            inboundValue,
            quantityBalance,
            valueBalance,
            averageCost,
            dto.SourceStatus,
            policy.CostingMethod,
            policy.ValuationScope,
            dto.ReferenceType,
            dto.ReferenceId,
            dto.ReferenceItemId,
            null);

        await _ledgerRepository.InsertAsync(ledgerEntry).ConfigureAwait(false);

        var layer = new InventoryCostLayer(
            Guid.NewGuid(),
            dto.ProductId,
            dto.WarehouseId,
            ledgerEntry.Id,
            dto.ReferenceType,
            dto.ReferenceId,
            dto.ReferenceItemId,
            occurredAtUtc,
            dto.Quantity,
            dto.Quantity,
            dto.UnitCost,
            inboundValue,
            dto.SourceStatus,
            policy.CostingMethod,
            policy.ValuationScope,
            null);

        await _layerRepository.InsertAsync(layer).ConfigureAwait(false);

        return new InventoryCostMovementResultDto
        {
            LedgerEntryId = ledgerEntry.Id,
            UnitCost = dto.UnitCost,
            TotalCost = inboundValue,
            Status = dto.SourceStatus
        };
    }

    public async Task AssignGoodsReceiptItemCostAsync(AssignGoodsReceiptItemCostDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        if (dto.UnitCost < 0)
            throw new InvalidInventoryCostingOperationException("Error.InventoryCosting.UnitCostCannotBeNegative");

        var layer = _layerReader.DataSource
            .Where(l => l.SourceReferenceType == InventoryCostReferenceType.GoodsReceipt
                     && l.SourceReferenceId == dto.GoodsReceiptId
                     && l.SourceReferenceItemId == dto.GoodsReceiptItemId
                     && l.CostingStatus != InventoryCostingStatus.Superseded)
            .OrderByDescending(l => l.OpenedAtUtc)
            .FirstOrDefault();

        if (layer is null)
            throw new InvalidInventoryCostingOperationException("Error.InventoryCosting.PendingLayerNotFound", dto.GoodsReceiptItemId);

        layer.SetCost(dto.UnitCost, InventoryCostingStatus.Final);
        await _layerRepository.UpdateAsync(layer).ConfigureAwait(false);
        await RevalueProductFromAsync(dto.ProductId, layer.OpenedAtUtc, InventoryCostRebuildTrigger.ReceiptCostAssigned, null).ConfigureAwait(false);
    }

    public Task<InventoryCostSummaryDto> GetCurrentCostSummaryAsync(Guid productId)
    {
        var balance = GetLastProductBalance(productId);
        var hasPendingCost = HasOpenPendingLayer(productId, DateTime.MaxValue);

        return Task.FromResult(new InventoryCostSummaryDto
        {
            ProductId = productId,
            QuantityBalance = balance.Quantity,
            ValueBalance = balance.Value,
            AverageCost = balance.AverageCost,
            Status = hasPendingCost ? InventoryCostingStatus.Pending : balance.Status
        });
    }

    public Task<InventoryCogsSummaryDto> GetCogsForReferencesAsync(InventoryCostReferenceType referenceType, IEnumerable<Guid> referenceIds)
    {
        var ids = referenceIds.Distinct().ToArray();
        if (ids.Length == 0)
        {
            return Task.FromResult(new InventoryCogsSummaryDto
            {
                TotalCost = 0,
                HasPendingCost = false,
                HasRevaluedCost = false
            });
        }

        var allocations = _allocationReader.DataSource
            .Where(a => a.OutboundReferenceType == referenceType
                     && ids.Contains(a.OutboundReferenceId)
                     && a.CostingStatus != InventoryCostingStatus.Superseded)
            .ToList();

        return Task.FromResult(new InventoryCogsSummaryDto
        {
            TotalCost = allocations.Sum(a => a.TotalCost ?? 0m),
            HasPendingCost = allocations.Any(a => a.CostingStatus == InventoryCostingStatus.Pending),
            HasRevaluedCost = allocations.Any(a => a.CostingStatus == InventoryCostingStatus.Revalued)
        });
    }

    public async Task<Guid> RevalueProductFromAsync(Guid productId, DateTime fromUtc, InventoryCostRebuildTrigger trigger, Guid? requestedByUserId)
    {
        var policy = await GetActivePolicyAsync().ConfigureAwait(false);
        EnsureWeightedAverage(policy);

        var run = new InventoryCostRebuildRun(
            Guid.NewGuid(),
            trigger,
            policy.CostingMethod,
            policy.ValuationScope,
            fromUtc,
            null,
            productId,
            requestedByUserId);

        await _rebuildRunRepository.InsertAsync(run).ConfigureAwait(false);

        try
        {
            var affectedEntries = _ledgerReader.DataSource
                .Where(l => l.ProductId == productId
                         && l.CostingStatus != InventoryCostingStatus.Superseded
                         && l.OccurredAtUtc >= fromUtc)
                .OrderBy(l => l.OccurredAtUtc)
                .ThenBy(l => l.SequenceNumber)
                .ToList();

            if (affectedEntries.Count == 0)
            {
                run.Complete();
                await _rebuildRunRepository.UpdateAsync(run).ConfigureAwait(false);
                return run.Id;
            }

            var affectedEntryIds = affectedEntries.Select(e => e.Id).ToHashSet();
            var affectedAllocations = _allocationReader.DataSource
                .Where(a => affectedEntryIds.Contains(a.OutboundLedgerEntryId)
                         && a.CostingStatus != InventoryCostingStatus.Superseded)
                .ToList();
            var affectedLayers = _layerReader.DataSource
                .Where(l => l.ProductId == productId
                         && l.CostingStatus != InventoryCostingStatus.Superseded
                         && (affectedEntryIds.Contains(l.SourceLedgerEntryId) || l.OpenedAtUtc >= fromUtc))
                .ToList();

            var sourceLayerCosts = affectedLayers
                .GroupBy(l => (l.SourceReferenceType, l.SourceReferenceId, l.SourceReferenceItemId))
                .ToDictionary(
                    g => g.Key,
                    g => g.OrderByDescending(l => l.OpenedAtUtc).First().UnitCost);

            foreach (var allocation in affectedAllocations)
            {
                allocation.MarkSuperseded(run.Id);
                await _allocationRepository.UpdateAsync(allocation).ConfigureAwait(false);
            }

            foreach (var layer in affectedLayers)
            {
                layer.MarkSuperseded(run.Id);
                await _layerRepository.UpdateAsync(layer).ConfigureAwait(false);
            }

            foreach (var entry in affectedEntries)
            {
                entry.MarkSuperseded(run.Id);
                await _ledgerRepository.UpdateAsync(entry).ConfigureAwait(false);
            }

            var replayState = GetReplayState(productId, fromUtc);
            foreach (var entry in affectedEntries)
            {
                if (entry.QuantityDelta >= 0)
                {
                    var key = (entry.ReferenceType, entry.ReferenceId, entry.ReferenceItemId);
                    var unitCost = entry.UnitCost ?? (sourceLayerCosts.TryGetValue(key, out var layerUnitCost) ? layerUnitCost : null);
                    await ReplayInboundAsync(entry, unitCost, run.Id, replayState).ConfigureAwait(false);
                }
                else
                {
                    await ReplayOutboundAsync(entry, run.Id, replayState).ConfigureAwait(false);
                }
            }

            run.Complete();
            await _rebuildRunRepository.UpdateAsync(run).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            run.Fail(ex.Message);
            await _rebuildRunRepository.UpdateAsync(run).ConfigureAwait(false);
            throw;
        }

        return run.Id;
    }

    public async Task<Guid> RebuildAllAsync(InventoryCostingMethod method, InventoryValuationScope scope, Guid? requestedByUserId)
    {
        if (method != InventoryCostingMethod.WeightedAverage)
            throw new UnsupportedInventoryCostingMethodException(method);

        var run = new InventoryCostRebuildRun(
            Guid.NewGuid(),
            InventoryCostRebuildTrigger.PolicyRebuild,
            method,
            scope,
            DateTime.MinValue,
            null,
            null,
            requestedByUserId);

        await _rebuildRunRepository.InsertAsync(run).ConfigureAwait(false);
        run.Complete();
        await _rebuildRunRepository.UpdateAsync(run).ConfigureAwait(false);
        return run.Id;
    }

    private async Task<InventoryCostingPolicy> GetActivePolicyAsync()
    {
        var policy = _policyReader.DataSource
            .Where(p => p.IsActive)
            .OrderByDescending(p => p.EffectiveFromUtc)
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

    private long GetNextSequenceNumber()
    {
        var lastSequence = _ledgerReader.DataSource
            .OrderByDescending(l => l.SequenceNumber)
            .Select(l => (long?)l.SequenceNumber)
            .FirstOrDefault();

        return (lastSequence ?? 0) + 1;
    }

    private ProductCostBalance GetLastProductBalance(Guid productId)
    {
        var lastEntry = _ledgerReader.DataSource
            .Where(l => l.ProductId == productId && l.CostingStatus != InventoryCostingStatus.Superseded)
            .OrderByDescending(l => l.OccurredAtUtc)
            .ThenByDescending(l => l.SequenceNumber)
            .FirstOrDefault();

        return lastEntry is null
            ? new ProductCostBalance(0, 0, 0, InventoryCostingStatus.Final)
            : new ProductCostBalance(lastEntry.QuantityBalanceAfter, lastEntry.ValueBalanceAfter, lastEntry.AverageCostAfter, lastEntry.CostingStatus);
    }

    private bool HasOpenPendingLayer(Guid productId, DateTime occurredAtUtc)
        => _layerReader.DataSource.Any(l => l.ProductId == productId
                                         && l.CostingStatus == InventoryCostingStatus.Pending
                                         && l.OpenedAtUtc <= occurredAtUtc
                                         && l.RemainingQuantity > 0);

    private ReplayState GetReplayState(Guid productId, DateTime fromUtc)
    {
        var previousEntry = _ledgerReader.DataSource
            .Where(l => l.ProductId == productId
                     && l.CostingStatus != InventoryCostingStatus.Superseded
                     && l.OccurredAtUtc < fromUtc)
            .OrderByDescending(l => l.OccurredAtUtc)
            .ThenByDescending(l => l.SequenceNumber)
            .FirstOrDefault();

        var pendingQuantity = _layerReader.DataSource
            .Where(l => l.ProductId == productId
                     && l.CostingStatus == InventoryCostingStatus.Pending
                     && l.OpenedAtUtc < fromUtc
                     && l.RemainingQuantity > 0)
            .Sum(l => l.RemainingQuantity);

        return previousEntry is null
            ? new ReplayState(0, 0, 0, pendingQuantity)
            : new ReplayState(previousEntry.QuantityBalanceAfter, previousEntry.ValueBalanceAfter, previousEntry.AverageCostAfter, pendingQuantity);
    }

    private async Task ReplayInboundAsync(InventoryCostLedgerEntry source, decimal? unitCost, Guid runId, ReplayState state)
    {
        var quantity = source.QuantityDelta;
        var totalCost = unitCost.HasValue ? quantity * unitCost.Value : (decimal?)null;

        state.Quantity += quantity;
        if (totalCost.HasValue)
            state.Value += totalCost.Value;
        else
            state.PendingQuantity += quantity;

        state.AverageCost = state.Quantity > 0 ? state.Value / state.Quantity : 0;
        var status = !unitCost.HasValue || source.CostingStatus == InventoryCostingStatus.Pending || state.PendingQuantity > 0
            ? InventoryCostingStatus.Pending
            : InventoryCostingStatus.Revalued;

        var ledgerEntry = new InventoryCostLedgerEntry(
            Guid.NewGuid(),
            source.ProductId,
            source.WarehouseId,
            source.OccurredAtUtc,
            GetNextSequenceNumber(),
            source.MovementType,
            quantity,
            unitCost,
            totalCost,
            state.Quantity,
            state.Value,
            state.AverageCost,
            status,
            source.CostingMethod,
            source.ValuationScope,
            source.ReferenceType,
            source.ReferenceId,
            source.ReferenceItemId,
            runId);

        await _ledgerRepository.InsertAsync(ledgerEntry).ConfigureAwait(false);

        var layer = new InventoryCostLayer(
            Guid.NewGuid(),
            source.ProductId,
            source.WarehouseId,
            ledgerEntry.Id,
            source.ReferenceType,
            source.ReferenceId,
            source.ReferenceItemId,
            source.OccurredAtUtc,
            quantity,
            quantity,
            unitCost,
            totalCost,
            status,
            source.CostingMethod,
            source.ValuationScope,
            runId);

        await _layerRepository.InsertAsync(layer).ConfigureAwait(false);
    }

    private async Task ReplayOutboundAsync(InventoryCostLedgerEntry source, Guid runId, ReplayState state)
    {
        var quantity = Math.Abs(source.QuantityDelta);
        var unitCost = state.AverageCost;
        var totalCost = quantity * unitCost;
        var status = source.CostingStatus == InventoryCostingStatus.Pending || state.PendingQuantity > 0
            ? InventoryCostingStatus.Pending
            : InventoryCostingStatus.Revalued;

        state.Quantity -= quantity;
        state.Value -= totalCost;
        state.PendingQuantity = Math.Max(0, state.PendingQuantity - quantity);

        if (state.Quantity <= 0)
        {
            state.Quantity = 0;
            state.Value = 0;
            state.AverageCost = 0;
        }
        else
        {
            state.AverageCost = state.Value / state.Quantity;
        }

        var ledgerEntry = new InventoryCostLedgerEntry(
            Guid.NewGuid(),
            source.ProductId,
            source.WarehouseId,
            source.OccurredAtUtc,
            GetNextSequenceNumber(),
            source.MovementType,
            -quantity,
            unitCost,
            totalCost,
            state.Quantity,
            state.Value,
            state.AverageCost,
            status,
            source.CostingMethod,
            source.ValuationScope,
            source.ReferenceType,
            source.ReferenceId,
            source.ReferenceItemId,
            runId);

        await _ledgerRepository.InsertAsync(ledgerEntry).ConfigureAwait(false);

        var allocation = new InventoryCostAllocation(
            Guid.NewGuid(),
            source.ProductId,
            source.WarehouseId,
            ledgerEntry.Id,
            source.ReferenceType,
            source.ReferenceId,
            source.ReferenceItemId,
            null,
            quantity,
            unitCost,
            totalCost,
            status,
            source.CostingMethod,
            source.ValuationScope,
            runId);

        await _allocationRepository.InsertAsync(allocation).ConfigureAwait(false);
    }

    private static void ValidatePositiveQuantity(decimal quantity)
    {
        if (quantity <= 0)
            throw new InvalidInventoryCostingOperationException("Error.InventoryCosting.QuantityMustBePositive");
    }

    private static void EnsureWeightedAverage(InventoryCostingPolicy policy)
    {
        if (policy.CostingMethod != InventoryCostingMethod.WeightedAverage)
            throw new UnsupportedInventoryCostingMethodException(policy.CostingMethod);
    }

    private sealed record ProductCostBalance(decimal Quantity, decimal Value, decimal AverageCost, InventoryCostingStatus Status);

    private sealed class ReplayState
    {
        public ReplayState(decimal quantity, decimal value, decimal averageCost, decimal pendingQuantity)
        {
            Quantity = quantity;
            Value = value;
            AverageCost = averageCost;
            PendingQuantity = pendingQuantity;
        }

        public decimal Quantity { get; set; }
        public decimal Value { get; set; }
        public decimal AverageCost { get; set; }
        public decimal PendingQuantity { get; set; }
    }
}
