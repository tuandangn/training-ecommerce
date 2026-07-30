using NamEcommerce.Application.Contracts.Dtos.Inventory;
using NamEcommerce.Application.Contracts.Dtos.PurchaseOrders;
using NamEcommerce.Application.Contracts.Inventory;
using NamEcommerce.Application.Contracts.PurchaseOrders;
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
    IPurchaseOrderAppService purchaseOrderAppService,
    IPurchaseOrderAllocationManager purchaseOrderAllocationManager,
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
        return await PrepareAggregatedShortagesAsync(shortageLines, filter).ConfigureAwait(false);
    }
    private async Task<ShortageAggregationAppDto> PrepareAggregatedShortagesAsync(List<ShortageLine> shortageLines, ShortageAggregationFilterAppDto filter)
    {
        if (shortageLines.Count == 0)
        {
            return new ShortageAggregationAppDto
            {
                Filter = filter
            };
        }

        var primaryOrderItemIds = await GetPrimaryShortageOrderItemIdsAsync(filter).ConfigureAwait(false);

        var products = (await productReader.GetByIdsAsync(shortageLines.Select(line => line.ProductId).Distinct()).ConfigureAwait(false))
            .ToDictionary(product => product.Id);
        var unitMeasurementIds = products.Values
            .Where(product => product.UnitMeasurementId.HasValue)
            .Select(product => product.UnitMeasurementId!.Value)
            .Distinct()
            .ToList();
        var unitMeasurementData = unitMeasurementReader.DataSource
            .Where(unitMeasurement => unitMeasurementIds.Contains(unitMeasurement.Id))
            .ToList();
        var unitMeasurements = unitMeasurementData.ToDictionary(u => u.Id, u => u.Name);
        var unitDecimalPlaces = unitMeasurementData.ToDictionary(u => u.Id, u => u.DecimalPlaces);
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
            var quantityDecimalPlaces = product?.UnitMeasurementId.HasValue == true
                                        && unitDecimalPlaces.TryGetValue(product.UnitMeasurementId!.Value, out var dp)
                ? dp : 0;

            var isFromPrimaryOrder = primaryOrderItemIds is null || primaryOrderItemIds.Contains(shortage.OrderItemId);

            var item = new ShortageAggregationItemAppDto
            {
                OrderCode = shortage.OrderCode,
                OrderId = shortage.OrderId,
                OrderItemId = shortage.OrderItemId,
                ProductId = shortage.ProductId,
                ProductName = shortage.ProductName,
                UnitMeasurementName = unitMeasurementName,
                QuantityDecimalPlaces = quantityDecimalPlaces,
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
                ShippingAddress = shortage.ShippingAddress,
                ShippingPhoneNumber = shortage.ShippingPhoneNumber,
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

    private async Task<HashSet<Guid>?> GetPrimaryShortageOrderItemIdsAsync(ShortageAggregationFilterAppDto filter)
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

    private sealed class CreatedPurchaseOrderItemBucket
    {
        public required SecondaryItemId PurchaseOrderItemId { get; init; }
        public required Guid ProductId { get; init; }
        public required decimal UnitCost { get; init; }
        public decimal RemainingQuantity { get; set; }
    }

    private async Task<CreatePoFromShortageResultDto> CreatePurchaseOrderFromShortageViaAppServiceAsync(
        Guid vendorId,
        Guid? warehouseId,
        DateTime? expectedDeliveryDateUtc,
        string? note,
        IList<CreatePoFromShortageItemDto> items)
    {
        var createResult = await purchaseOrderAppService.CreatePurchaseOrderAsync(new CreatePurchaseOrderAppDto
        {
            PlacedOnUtc = DateTime.UtcNow,
            VendorId = vendorId,
            WarehouseId = warehouseId,
            ExpectedDeliveryDateUtc = expectedDeliveryDateUtc,
            Note = note,
            Items = items.Select(item => new CreatePurchaseOrderItemAppDto
            {
                ProductId = item.ProductId,
                Quantity = item.Quantity,
                UnitCost = item.UnitCost,
                Note = item.Note
            }).ToList()
        }).ConfigureAwait(false);

        if (!createResult.Success || !createResult.CreatedId.HasValue)
            throw new InvalidOperationException(createResult.ErrorMessage ?? "Error.PurchaseOrderCreateFailed");

        var purchaseOrder = await purchaseOrderAppService.GetPurchaseOrderByIdAsync(createResult.CreatedId.Value).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Error.PurchaseOrderIsNotFound");

        await AllocateCreatedPurchaseOrderItemsAsync(purchaseOrder, items).ConfigureAwait(false);

        return new CreatePoFromShortageResultDto
        {
            PurchaseOrderId = purchaseOrder.Id,
            PurchaseOrderCode = purchaseOrder.Code,
            CreatedNew = true
        };
    }

    private async Task AllocateCreatedPurchaseOrderItemsAsync(PurchaseOrderAppDto purchaseOrder, IList<CreatePoFromShortageItemDto> items)
    {
        var buckets = purchaseOrder.Items
            .Select(item => new CreatedPurchaseOrderItemBucket
            {
                PurchaseOrderItemId = (purchaseOrder.Id, item.Id),
                ProductId = item.ProductId,
                UnitCost = item.UnitCost,
                RemainingQuantity = item.QuantityOrdered
            })
            .ToList();

        foreach (var item in items)
        {
            var remainingAllocationQuantity = item.AllocationQuantity ?? item.Quantity;
            if (remainingAllocationQuantity <= 0)
                continue;

            foreach (var bucket in buckets.Where(bucket =>
                         bucket.ProductId == item.ProductId
                         && bucket.UnitCost == item.UnitCost
                         && bucket.RemainingQuantity > 0))
            {
                var allocationQuantity = Math.Min(remainingAllocationQuantity, bucket.RemainingQuantity);
                var allocation = await purchaseOrderAllocationManager
                    .AllocatePurchaseOrderItemForOrder(new AllocatePurchaseOrderItemForOrder
                    {
                        PurchaseOrderItemId = bucket.PurchaseOrderItemId,
                        OrderItemId = item.OrderItemId,
                        AllocationQuantity = allocationQuantity,
                        DirectShipInfo = item.DirectShipInfo is not null
                            ? new AllocatePurchaseOrderItemForOrder.AllocateDirectShipInfo(item.DirectShipInfo.ContactName, item.DirectShipInfo.ContactPhone, item.DirectShipInfo.Address)
                            {
                                Priority = item.DirectShipInfo.Priority
                            } : null
                    })
                    .ConfigureAwait(false);

                bucket.RemainingQuantity -= allocationQuantity;
                remainingAllocationQuantity -= allocationQuantity;
                if (remainingAllocationQuantity <= 0)
                    break;
            }

            if (remainingAllocationQuantity > 0)
                throw new InvalidOperationException("Error.PurchaseOrderItemIsNotFound");
        }
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
                        result = await CreatePurchaseOrderFromShortageViaAppServiceAsync(
                            group.VendorId.Value,
                            group.WarehouseId,
                            group.ExpectedDeliveryDateUtc,
                            group.Note,
                            items).ConfigureAwait(false);
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
                    var remainingAllocationQuantity = item.AllocationQuantity ?? item.Quantity;
                    foreach (var action in item.Actions.Where(action => action.Quantity > 0))
                    {
                        var actionAllocationQuantity = Math.Min(remainingAllocationQuantity, action.Quantity);
                        remainingAllocationQuantity -= actionAllocationQuantity;
                        var actionType = NormalizeActionType(action.ActionType);
                        if (actionType == "allocate")
                        {
                            if (!action.PurchaseOrderItemId.HasValue || !action.PurchaseOrderId.HasValue)
                                throw new InvalidOperationException("Error.PurchaseOrderItemIsNotFound");

                            if (actionAllocationQuantity > 0)
                            {
                                var allocationDto = await purchaseOrderAllocationManager
                                    .AllocatePurchaseOrderItemForOrder(new AllocatePurchaseOrderItemForOrder
                                    {
                                        PurchaseOrderItemId = (action.PurchaseOrderId.Value, action.PurchaseOrderItemId.Value),
                                        OrderItemId = (item.OrderId, item.OrderItemId),
                                        AllocationQuantity = actionAllocationQuantity,
                                        DirectShipInfo = item.DirectShipInfo is not null
                                            ? new AllocatePurchaseOrderItemForOrder.AllocateDirectShipInfo(item.DirectShipInfo.ContactName, item.DirectShipInfo.ContactPhone, item.DirectShipInfo.Address)
                                            {
                                                Priority = item.DirectShipInfo.Priority
                                            } : null
                                    })
                                    .ConfigureAwait(false);
                            }

                            await AddExistingPurchaseOrderResultAsync(results, action.PurchaseOrderId.Value, false).ConfigureAwait(false);
                            continue;
                        }

                        var domainItem = ToDomainShortageItem(item, action.Quantity, action.UnitCost, actionAllocationQuantity);
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
                    var result = await CreatePurchaseOrderFromShortageViaAppServiceAsync(
                        group.VendorId.Value,
                        group.WarehouseId,
                        group.ExpectedDeliveryDateUtc,
                        group.Note,
                        newItems).ConfigureAwait(false);
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
        decimal? unitCost = null,
        decimal? allocationQuantity = null)
    {
        var finalQuantity = quantity ?? item.Quantity;
        var finalAllocationQuantity = allocationQuantity ?? item.AllocationQuantity ?? finalQuantity;

        return new()
        {
            OrderItemId = (item.OrderId, item.OrderItemId),
            ProductId = item.ProductId,
            Quantity = finalQuantity,
            AllocationQuantity = finalAllocationQuantity,
            UnitCost = unitCost ?? item.UnitCost,
            Note = item.Note,
            DirectShipInfo = finalAllocationQuantity > 0 && item.DirectShipInfo is { } ds
                ? new DirectShipInfoDto { Address = ds.Address, ContactName = ds.ContactName, ContactPhone = ds.ContactPhone, Priority = ds.Priority }
                : null
        };
    }

    private static void ValidateActionQuantity(CreatePoFromShortageItemAppDto item)
    {
        var totalActionQuantity = item.Actions.Sum(action => action.Quantity);
        if (item.Quantity <= 0 || Math.Abs(totalActionQuantity - item.Quantity) > 0.0001m)
            throw new InvalidOperationException("Error.PurchaseOrderShortageActionQuantityInvalid");

        var allocationQuantity = item.AllocationQuantity ?? item.Quantity;
        if (allocationQuantity < 0 || allocationQuantity > item.Quantity)
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
                    shortage.OrderId,
                    shortage.OrderItemId,
                    shortage.OrderCode,
                    shortage.ProductId,
                    shortage.ProductName,
                    shortage.RequiredQuantity,
                    shortage.ShippedQuantity,
                    shortage.AvailableQuantity,
                    shortage.ShortageQuantity,
                    null, null, null,
                    shortage.ShippingAddress, shortage.ShippingPhoneNumber,
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
                shortage.OrderId,
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
                shortage.ShippingAddress, shortage.ShippingPhoneNumber,
                shortage.AllocatedFromPurchaseOrders))
            .ToList();
    }

    private sealed record ShortageLine(
        Guid OrderId,
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
        string ShippingAddress,
        string? ShippingPhoneNumber,
        IList<PurchaseOrderShortageAllocationDto> AllocatedFromPurchaseOrders);
}
