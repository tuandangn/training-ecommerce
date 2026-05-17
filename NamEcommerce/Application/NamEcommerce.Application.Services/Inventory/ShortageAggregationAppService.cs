using NamEcommerce.Application.Contracts.Dtos.Inventory;
using NamEcommerce.Application.Contracts.Inventory;
using NamEcommerce.Domain.Entities.Catalog;
using NamEcommerce.Domain.Entities.DeliveryNotes;
using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Domain.Shared.Dtos.Inventory;
using NamEcommerce.Domain.Shared.Dtos.PurchaseOrders;
using NamEcommerce.Domain.Shared.Enums.PurchaseOrders;
using NamEcommerce.Domain.Shared.Exceptions.DeliveryNotes;
using NamEcommerce.Domain.Shared.Services.Inventory;
using NamEcommerce.Domain.Shared.Services.PurchaseOrders;

namespace NamEcommerce.Application.Services.Inventory;

public sealed class ShortageAggregationAppService(
    IShortageQueryService shortageQueryService,
    ISupplierSuggestionService supplierSuggestionService,
    IPurchaseOrderManager purchaseOrderManager,
    IPurchaseOrderAllocationManager purchaseOrderAllocationManager,
    IDirectShipManager directShipManager,
    IEntityDataReader<DeliveryNote> deliveryNoteReader,
    IEntityDataReader<Product> productReader,
    IEntityDataReader<UnitMeasurement> unitMeasurementReader) : IShortageAggregationAppService
{
    public async Task<ShortageAggregationAppDto> GetAggregatedShortagesAsync(ShortageAggregationFilterAppDto filter)
    {
        var shortageLines = await GetShortageLinesAsync(filter).ConfigureAwait(false);
        if (shortageLines.Count == 0)
        {
            return new ShortageAggregationAppDto
            {
                Filter = filter
            };
        }

        var primaryOrderItemIds = await GetPrimaryOrderItemIdsAsync(filter).ConfigureAwait(false);

        var products = (await productReader.GetByIdsAsync(shortageLines.Select(line => line.ProductId).Distinct()).ConfigureAwait(false))
            .ToDictionary(product => product.Id);
        var unitMeasurementIds = products.Values
            .Where(product => product.UnitMeasurementId.HasValue)
            .Select(product => product.UnitMeasurementId!.Value)
            .Distinct()
            .ToList();
        var unitMeasurements = unitMeasurementReader.DataSource
            .Where(unitMeasurement => unitMeasurementIds.Contains(unitMeasurement.Id))
            .ToDictionary(unitMeasurement => unitMeasurement.Id, unitMeasurement => unitMeasurement.Name);
        var groups = new Dictionary<Guid, VendorShortageGroupAppDto>();
        var noVendorItems = new List<ShortageAggregationItemAppDto>();

        foreach (var shortage in shortageLines)
        {
            var suggestions = await supplierSuggestionService.SuggestVendorsForProductAsync(shortage.ProductId).ConfigureAwait(false);
            var primarySuggestion = suggestions.FirstOrDefault();
            var unitCost = primarySuggestion?.LastUnitPrice
                ?? (products.TryGetValue(shortage.ProductId, out var product) ? product.CostPrice : 0);
            var unitMeasurementName = products.TryGetValue(shortage.ProductId, out product)
                                      && product.UnitMeasurementId.HasValue
                                      && unitMeasurements.TryGetValue(product.UnitMeasurementId.Value, out var unitName)
                ? unitName
                : null;

            var isFromPrimaryOrder = primaryOrderItemIds is null || primaryOrderItemIds.Contains(shortage.OrderItemId);

            var item = new ShortageAggregationItemAppDto
            {
                OrderCode = shortage.OrderCode,
                OrderItemId = shortage.OrderItemId,
                ProductId = shortage.ProductId,
                ProductName = shortage.ProductName,
                UnitMeasurementName = unitMeasurementName,
                RequiredQuantity = shortage.RequiredQuantity,
                ShippedQuantity = shortage.ShippedQuantity,
                AvailableQuantity = shortage.AvailableQuantity,
                ShortageQuantity = shortage.ShortageQuantity,
                QuantityToOrder = shortage.ShortageQuantity,
                UnitCost = unitCost,
                IsFromPrimaryOrder = isFromPrimaryOrder,
                CustomerName = shortage.CustomerName,
                CustomerPhone = shortage.CustomerPhone,
                CustomerAddress = shortage.CustomerAddress,
                SupplierSuggestions = suggestions.Select(suggestion => new SupplierSuggestionAppDto
                {
                    VendorId = suggestion.VendorId,
                    VendorName = suggestion.VendorName,
                    DisplayOrder = suggestion.DisplayOrder,
                    LastPurchaseDateUtc = suggestion.LastPurchaseDateUtc,
                    LastUnitPrice = suggestion.LastUnitPrice,
                    IsPreferred = suggestion.IsPreferred
                }).ToList(),
                AllocatedFromPurchaseOrders = shortage.AllocatedFromPurchaseOrders.Select(allocation => new PurchaseOrderShortageAllocationAppDto
                {
                    PurchaseOrderId = allocation.POId,
                    PurchaseOrderCode = allocation.POCode,
                    AllocatedQuantity = allocation.AllocatedQty,
                    ReceivedQuantity = allocation.ReceivedQty,
                    ExpectedReceiveDateUtc = allocation.ExpectedReceiveDateUtc
                }).ToList()
            };

            if (primarySuggestion is null)
            {
                noVendorItems.Add(item);
                continue;
            }

            if (!groups.TryGetValue(primarySuggestion.VendorId, out var group))
            {
                group = new VendorShortageGroupAppDto
                {
                    VendorId = primarySuggestion.VendorId,
                    VendorName = primarySuggestion.VendorName
                };
                groups[primarySuggestion.VendorId] = group;
            }

            group.Items.Add(item);
        }

        var vendorGroups = groups.Values.OrderBy(group => group.VendorName).ToList();

        if (primaryOrderItemIds is not null)
        {
            vendorGroups = vendorGroups
                .Where(group => group.Items.Any(item => item.IsFromPrimaryOrder))
                .ToList();
        }

        if (noVendorItems.Count > 0)
        {
            var filteredNoVendorItems = primaryOrderItemIds is null
                ? noVendorItems
                : noVendorItems.Where(item => item.IsFromPrimaryOrder).ToList();
            if (filteredNoVendorItems.Count > 0)
            {
                vendorGroups.Add(new VendorShortageGroupAppDto
                {
                    VendorName = "Chưa có nhà cung cấp",
                    IsNoVendorGroup = true,
                    Items = filteredNoVendorItems
                });
            }
        }

        return new ShortageAggregationAppDto
        {
            Filter = filter,
            VendorGroups = vendorGroups
        };
    }

    private async Task<HashSet<Guid>?> GetPrimaryOrderItemIdsAsync(ShortageAggregationFilterAppDto filter)
    {
        if (filter.OrderId is Guid orderId)
        {
            var primaryShortages = await shortageQueryService.GetOrderItemShortagesAsync(orderId).ConfigureAwait(false);
            return primaryShortages.Select(shortage => shortage.OrderItemId).ToHashSet();
        }

        if (filter.DeliveryNoteId is Guid deliveryNoteId && !filter.IncludeSalesOrderScope)
        {
            var deliveryNoteShortages = await shortageQueryService.GetDeliveryNoteShortagesAsync(deliveryNoteId).ConfigureAwait(false);
            return deliveryNoteShortages.Select(shortage => shortage.OrderItemId).ToHashSet();
        }

        return null;
    }

    public async Task<IList<ExistingDraftPurchaseOrderAppDto>> CheckExistingDraftsAsync(IList<Guid> vendorIds)
    {
        var result = new List<ExistingDraftPurchaseOrderAppDto>();
        foreach (var vendorId in vendorIds.Where(id => id != Guid.Empty).Distinct())
        {
            var draft = await purchaseOrderManager.FindDraftForVendorAsync(vendorId).ConfigureAwait(false);
            if (draft is null)
                continue;

            result.Add(new ExistingDraftPurchaseOrderAppDto
            {
                Id = draft.Id,
                VendorId = draft.VendorId,
                Code = draft.Code,
                CreatedOnUtc = draft.CreatedOnUtc,
                ExpectedDeliveryDateUtc = draft.ExpectedDeliveryDateUtc
            });
        }

        return result;
    }

    public async Task<IList<RelatedPurchaseOrderAppDto>> CheckRelatedPurchaseOrdersAsync(CheckRelatedPurchaseOrdersAppDto dto)
    {
        var result = new List<RelatedPurchaseOrderAppDto>();
        var productIds = dto.ProductIds.Where(id => id != Guid.Empty).Distinct().ToList();
        if (productIds.Count == 0)
            return result;

        var statuses = new[]
        {
            PurchaseOrderStatus.Draft,
            PurchaseOrderStatus.Submitted,
            PurchaseOrderStatus.Approved
        };

        foreach (var vendorId in dto.VendorIds.Where(id => id != Guid.Empty).Distinct())
        {
            var purchaseOrders = await purchaseOrderManager
                .FindRelatedPurchaseOrdersAsync(vendorId, productIds, statuses)
                .ConfigureAwait(false);

            result.AddRange(purchaseOrders.Select(purchaseOrder => new RelatedPurchaseOrderAppDto
            {
                Id = purchaseOrder.Id,
                VendorId = purchaseOrder.VendorId,
                VendorName = purchaseOrder.VendorName,
                Code = purchaseOrder.Code,
                Status = (int)purchaseOrder.Status,
                CreatedOnUtc = purchaseOrder.CreatedOnUtc,
                ExpectedDeliveryDateUtc = purchaseOrder.ExpectedDeliveryDateUtc,
                TotalAmount = purchaseOrder.TotalAmount,
                ItemCount = purchaseOrder.ItemCount,
                CanAllocate = purchaseOrder.CanAllocate,
                CanMerge = purchaseOrder.CanMerge,
                Items = purchaseOrder.Items.Select(item => new RelatedPurchaseOrderItemAppDto
                {
                    PurchaseOrderItemId = item.PurchaseOrderItemId,
                    ProductId = item.ProductId,
                    ProductName = item.ProductName,
                    QuantityOrdered = item.QuantityOrdered,
                    AllocatedQuantity = item.AllocatedQuantity,
                    ReceivedQuantity = item.ReceivedQuantity,
                    AvailableForAllocation = item.AvailableForAllocation,
                    UnitCost = item.UnitCost
                }).ToList()
            }));
        }

        return result
            .OrderBy(purchaseOrder => purchaseOrder.VendorName)
            .ThenByDescending(purchaseOrder => purchaseOrder.CreatedOnUtc)
            .ToList();
    }

    public async Task<CreatePosFromShortageResultAppDto> CreatePurchaseOrdersFromShortageAsync(CreatePosFromShortageAppDto dto)
    {
        try
        {
            var results = new Dictionary<Guid, CreatedPurchaseOrderResultAppDto>();
            foreach (var group in dto.Groups.Where(group => group.Items.Count > 0))
            {
                if (!group.VendorId.HasValue)
                {
                    return new CreatePosFromShortageResultAppDto
                    {
                        Success = false,
                        ErrorMessage = "Error.VendorIsNotFound"
                    };
                }

                var legacyItems = group.Items.Where(item => item.Actions.Count == 0).ToList();
                if (legacyItems.Count > 0)
                {
                    var items = legacyItems.Select(item => ToDomainShortageItem(item)).ToList();
                    CreatePoFromShortageResultDto result;
                    if (group.MergeIntoExistingPoId.HasValue)
                    {
                        result = await purchaseOrderManager.AddItemsToExistingDraftAsync(group.MergeIntoExistingPoId.Value, items).ConfigureAwait(false);
                    }
                    else
                    {
                        result = await purchaseOrderManager.CreatePurchaseOrderFromShortageAsync(new CreatePoFromShortageDto
                        {
                            VendorId = group.VendorId.Value,
                            WarehouseId = group.WarehouseId,
                            ExpectedDeliveryDateUtc = group.ExpectedDeliveryDateUtc,
                            Note = group.Note,
                            Items = items
                        }).ConfigureAwait(false);
                    }

                    AddResult(results, new CreatedPurchaseOrderResultAppDto
                    {
                        PurchaseOrderId = result.PurchaseOrderId,
                        PurchaseOrderCode = result.PurchaseOrderCode,
                        CreatedNew = result.CreatedNew,
                        VendorId = group.VendorId.Value
                    });
                }

                var mergeItemsByPurchaseOrder = new Dictionary<Guid, List<CreatePoFromShortageItemDto>>();
                var newItems = new List<CreatePoFromShortageItemDto>();
                foreach (var item in group.Items.Where(item => item.Actions.Count > 0))
                {
                    ValidateActionQuantity(item);
                    foreach (var action in item.Actions.Where(action => action.Quantity > 0))
                    {
                        var actionType = NormalizeActionType(action.ActionType);
                        if (actionType == "allocate")
                        {
                            if (!action.PurchaseOrderItemId.HasValue || !action.PurchaseOrderId.HasValue)
                                throw new InvalidOperationException("Error.PurchaseOrderItemIsNotFound");

                            var allocationDto = await purchaseOrderAllocationManager
                                .AllocateFromExistingPurchaseOrderItemAsync(action.PurchaseOrderItemId.Value, item.OrderItemId, action.Quantity)
                                .ConfigureAwait(false);

                            if (item.DirectShipInfo is { } ds && !string.IsNullOrWhiteSpace(ds.Address))
                                await directShipManager
                                    .MarkAllocationAsDirectShipAsync(allocationDto.Id, ds.Address, ds.ContactName, ds.ContactPhone, ds.Priority)
                                    .ConfigureAwait(false);

                            await AddExistingPurchaseOrderResultAsync(results, action.PurchaseOrderId.Value, false).ConfigureAwait(false);
                            continue;
                        }

                        var domainItem = ToDomainShortageItem(item, action.Quantity, action.UnitCost);
                        if (actionType == "merge")
                        {
                            if (!action.PurchaseOrderId.HasValue)
                                throw new InvalidOperationException("Error.PurchaseOrderIsNotFound");

                            if (!mergeItemsByPurchaseOrder.TryGetValue(action.PurchaseOrderId.Value, out var mergeItems))
                            {
                                mergeItems = [];
                                mergeItemsByPurchaseOrder[action.PurchaseOrderId.Value] = mergeItems;
                            }

                            mergeItems.Add(domainItem);
                            continue;
                        }

                        if (actionType == "new")
                        {
                            newItems.Add(domainItem);
                            continue;
                        }

                        throw new InvalidOperationException("Error.PurchaseOrderShortageActionInvalid");
                    }
                }

                foreach (var mergeGroup in mergeItemsByPurchaseOrder)
                {
                    var result = await purchaseOrderManager
                        .AddItemsToExistingDraftAsync(mergeGroup.Key, mergeGroup.Value)
                        .ConfigureAwait(false);
                    AddResult(results, new CreatedPurchaseOrderResultAppDto
                    {
                        PurchaseOrderId = result.PurchaseOrderId,
                        PurchaseOrderCode = result.PurchaseOrderCode,
                        CreatedNew = result.CreatedNew,
                        VendorId = group.VendorId.Value
                    });
                }

                if (newItems.Count > 0)
                {
                    var result = await purchaseOrderManager.CreatePurchaseOrderFromShortageAsync(new CreatePoFromShortageDto
                    {
                        VendorId = group.VendorId.Value,
                        WarehouseId = group.WarehouseId,
                        ExpectedDeliveryDateUtc = group.ExpectedDeliveryDateUtc,
                        Note = group.Note,
                        Items = newItems
                    }).ConfigureAwait(false);
                    AddResult(results, new CreatedPurchaseOrderResultAppDto
                    {
                        PurchaseOrderId = result.PurchaseOrderId,
                        PurchaseOrderCode = result.PurchaseOrderCode,
                        CreatedNew = result.CreatedNew,
                        VendorId = group.VendorId.Value
                    });
                }
            }

            return new CreatePosFromShortageResultAppDto
            {
                Success = true,
                Items = results.Values.ToList()
            };
        }
        catch (Exception ex)
        {
            return new CreatePosFromShortageResultAppDto
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    private static CreatePoFromShortageItemDto ToDomainShortageItem(
        CreatePoFromShortageItemAppDto item,
        decimal? quantity = null,
        decimal? unitCost = null)
        => new()
        {
            OrderItemId = item.OrderItemId,
            ProductId = item.ProductId,
            Quantity = quantity ?? item.Quantity,
            UnitCost = unitCost ?? item.UnitCost,
            Note = item.Note,
            DirectShipInfo = item.DirectShipInfo is { } ds
                ? new DirectShipInfoDto { Address = ds.Address, ContactName = ds.ContactName, ContactPhone = ds.ContactPhone, Priority = ds.Priority }
                : null
        };

    private static void ValidateActionQuantity(CreatePoFromShortageItemAppDto item)
    {
        var totalActionQuantity = item.Actions.Sum(action => action.Quantity);
        if (item.Quantity <= 0 || Math.Abs(totalActionQuantity - item.Quantity) > 0.0001m)
            throw new InvalidOperationException("Error.PurchaseOrderShortageActionQuantityInvalid");
    }

    private static string NormalizeActionType(string actionType)
        => actionType.Trim().ToLowerInvariant() switch
        {
            "allocate" or "allocatefromexisting" or "allocatefromexistingpurchaseorderitem" => "allocate",
            "merge" or "mergeintodraft" => "merge",
            "new" or "createnew" => "new",
            _ => string.Empty
        };

    private static void AddResult(
        IDictionary<Guid, CreatedPurchaseOrderResultAppDto> results,
        CreatedPurchaseOrderResultAppDto result)
    {
        if (results.TryGetValue(result.PurchaseOrderId, out var existing) && existing.CreatedNew)
            return;

        results[result.PurchaseOrderId] = result;
    }

    private async Task AddExistingPurchaseOrderResultAsync(
        IDictionary<Guid, CreatedPurchaseOrderResultAppDto> results,
        Guid purchaseOrderId,
        bool createdNew)
    {
        if (results.ContainsKey(purchaseOrderId))
            return;

        var purchaseOrder = await purchaseOrderManager.GetPurchaseOrderByIdAsync(purchaseOrderId).ConfigureAwait(false);
        if (purchaseOrder is null)
            return;

        results[purchaseOrderId] = new CreatedPurchaseOrderResultAppDto
        {
            PurchaseOrderId = purchaseOrder.Id,
            PurchaseOrderCode = purchaseOrder.Code,
            CreatedNew = createdNew,
            VendorId = purchaseOrder.VendorId
        };
    }

    private async Task<List<ShortageLine>> GetShortageLinesAsync(ShortageAggregationFilterAppDto filter)
    {
        if (filter.DeliveryNoteId.HasValue && !filter.IncludeSalesOrderScope)
        {
            var deliveryNoteShortages = await shortageQueryService.GetDeliveryNoteShortagesAsync(filter.DeliveryNoteId.Value).ConfigureAwait(false);
            return deliveryNoteShortages
                .Where(shortage => !filter.ProductId.HasValue || shortage.ProductId == filter.ProductId.Value)
                .Select(shortage => new ShortageLine(
                    shortage.OrderItemId,
                    shortage.OrderCode,
                    shortage.ProductId,
                    shortage.ProductName,
                    shortage.RequiredQuantity,
                    shortage.ShippedQuantity,
                    shortage.AvailableQuantity,
                    shortage.ShortageQuantity,
                    null, null, null,
                    shortage.AllocatedFromPurchaseOrders))
                .ToList();
        }

        if (filter.DeliveryNoteId.HasValue && filter.IncludeSalesOrderScope)
        {
            _ = deliveryNoteReader.DataSource.SingleOrDefault(note => note.Id == filter.DeliveryNoteId.Value)
                ?? throw new DeliveryNoteNotFoundException(filter.DeliveryNoteId.Value);
        }

        var domainFilter = new ShortageFilterDto
        {
            ProductId = filter.ProductId
        };

        var shortages = await shortageQueryService.GetGlobalShortagesAsync(domainFilter).ConfigureAwait(false);
        return shortages
            .Select(shortage => new ShortageLine(
                shortage.OrderItemId,
                shortage.OrderCode,
                shortage.ProductId,
                shortage.ProductName,
                shortage.RequiredQuantity,
                shortage.ShippedQuantity,
                shortage.AvailableQuantity,
                shortage.ShortageQuantity,
                shortage.CustomerName,
                shortage.CustomerPhone,
                shortage.CustomerAddress,
                shortage.AllocatedFromPurchaseOrders))
            .ToList();
    }

    private sealed record ShortageLine(
        Guid OrderItemId,
        string OrderCode,
        Guid ProductId,
        string ProductName,
        decimal RequiredQuantity,
        decimal ShippedQuantity,
        decimal AvailableQuantity,
        decimal ShortageQuantity,
        string? CustomerName,
        string? CustomerPhone,
        string? CustomerAddress,
        IList<PurchaseOrderShortageAllocationDto> AllocatedFromPurchaseOrders);
}
