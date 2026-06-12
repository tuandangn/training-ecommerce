using NamEcommerce.Application.Contracts.Dtos.Returns;
using NamEcommerce.Application.Contracts.Returns;
using NamEcommerce.Application.Services.Extensions;
using NamEcommerce.Domain.Entities.Catalog;
using NamEcommerce.Domain.Entities.GoodsReceipts;
using NamEcommerce.Domain.Entities.Inventory;
using NamEcommerce.Domain.Entities.Returns;
using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Domain.Shared.Dtos.Returns;
using NamEcommerce.Domain.Shared.Enums.GoodsReceipts;
using NamEcommerce.Domain.Shared.Exceptions.Returns;
using NamEcommerce.Domain.Shared.Helpers;
using NamEcommerce.Domain.Shared.Services.Returns;

namespace NamEcommerce.Application.Services.Returns;

public sealed class VendorReturnAppService(IVendorReturnManager manager,
    IEntityDataReader<GoodsReceipt> goodsReceiptDataReader,
    IEntityDataReader<VendorReturn> vendorReturnDataReader,
    IEntityDataReader<Product> productDataReader,
    IEntityDataReader<UnitMeasurement> unitMeasurementDataReader,
    IEntityDataReader<Warehouse> warehouseDataReader,
    IEntityDataReader<InventoryStock> inventoryStockDataReader) : IVendorReturnAppService
{
    private readonly IVendorReturnManager _manager = manager;

    public async Task<CreateVendorReturnResultAppDto> CreateAsync(CreateVendorReturnAppDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var (valid, errorMessage) = dto.Validate();
        if (!valid)
        {
            return new CreateVendorReturnResultAppDto
            {
                Success = false,
                ErrorMessage = errorMessage
            };
        }

        foreach (var item in dto.Items)
        {
            var product = await productDataReader.GetByIdAsync(item.ProductId, default).ConfigureAwait(false);
            if (product is null)
            {
                return new CreateVendorReturnResultAppDto
                {
                    Success = false,
                    ErrorMessage = "Error.GoodsReceipt.ProductIsNotFound"
                };
            }

            if (product.UnitMeasurementId.HasValue)
            {
                var unitMeasurement = await unitMeasurementDataReader.GetByIdAsync(product.UnitMeasurementId.Value, default).ConfigureAwait(false);
                if (unitMeasurement is not null)
                {
                    if (new[] { item.AcceptedQuantity, item.RequestedQuantity }.Any(quantity => !NumberHelper.IsValidDecimalPlace(quantity, unitMeasurement.DecimalPlaces)))
                    {
                        return new CreateVendorReturnResultAppDto
                        {
                            Success = false,
                            ErrorMessage = "Error.QuantityMustBeInteger"
                        };
                    }
                }
            }
        }

        try
        {
            var domainDto = new CreateVendorReturnDto
            {
                VendorId = dto.VendorId,
                GoodsReceiptId = dto.GoodsReceiptId,
                WarehouseId = dto.WarehouseId,
                Note = dto.Note,
                AdditionalCost = dto.AdditionalCost,
                Items = dto.Items.Select(i => new CreateVendorReturnItemDto
                {
                    ProductId = i.ProductId,
                    GoodsReceiptItemId = i.GoodsReceiptItemId,
                    RequestedQuantity = i.RequestedQuantity,
                    AcceptedQuantity = i.AcceptedQuantity,
                    OriginalUnitCost = i.OriginalUnitCost,
                    ReturnUnitCost = i.ReturnUnitCost
                })
            };

            var result = await _manager.CreateAsync(domainDto).ConfigureAwait(false);
            return new CreateVendorReturnResultAppDto { Success = true, CreatedId = result.Id };
        }
        catch (ReturnDataIsInvalidException ex)
        {
            return new CreateVendorReturnResultAppDto { Success = false, ErrorMessage = ex.Message };
        }
        catch (Exception ex)
        {
            return new CreateVendorReturnResultAppDto { Success = false, ErrorMessage = ex.Message };
        }
    }

    public async Task<UpdateVendorReturnResultAppDto> UpdateAsync(UpdateVendorReturnAppDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        try
        {
            var domainDto = new UpdateVendorReturnDto(dto.Id)
            {
                Note = dto.Note,
                ReturnDate = dto.ReturnDate
            };

            await _manager.UpdateAsync(domainDto).ConfigureAwait(false);
            return new UpdateVendorReturnResultAppDto { Success = true };
        }
        catch (VendorReturnNotFoundException ex)
        {
            return new UpdateVendorReturnResultAppDto { Success = false, ErrorMessage = ex.Message };
        }
        catch (Exception ex)
        {
            return new UpdateVendorReturnResultAppDto { Success = false, ErrorMessage = ex.Message };
        }
    }

    public async Task<ConfirmVendorReturnResultAppDto> MoveToInspectingAsync(Guid id)
    {
        try
        {
            await _manager.MoveToInspectingAsync(id).ConfigureAwait(false);
            return new ConfirmVendorReturnResultAppDto { Success = true };
        }
        catch (VendorReturnNotFoundException ex)
        {
            return new ConfirmVendorReturnResultAppDto { Success = false, ErrorMessage = ex.Message };
        }
        catch (ReturnCannotChangeStatusException ex)
        {
            return new ConfirmVendorReturnResultAppDto { Success = false, ErrorMessage = ex.Message };
        }
        catch (Exception ex)
        {
            return new ConfirmVendorReturnResultAppDto { Success = false, ErrorMessage = ex.Message };
        }
    }

    public async Task<ConfirmVendorReturnResultAppDto> ConfirmAsync(Guid id, Guid? warehouseId = null)
    {
        try
        {
            await _manager.ConfirmAsync(id, warehouseId).ConfigureAwait(false);
            return new ConfirmVendorReturnResultAppDto { Success = true };
        }
        catch (VendorReturnNotFoundException ex)
        {
            return new ConfirmVendorReturnResultAppDto { Success = false, ErrorMessage = ex.Message };
        }
        catch (ReturnCannotChangeStatusException ex)
        {
            return new ConfirmVendorReturnResultAppDto { Success = false, ErrorMessage = ex.Message };
        }
        catch (ExceedsReceivedQuantityException ex)
        {
            return new ConfirmVendorReturnResultAppDto { Success = false, ErrorMessage = ex.Message };
        }
        catch (Exception ex)
        {
            return new ConfirmVendorReturnResultAppDto { Success = false, ErrorMessage = ex.Message };
        }
    }

    public async Task<ConfirmVendorReturnResultAppDto> CancelAsync(Guid id)
    {
        try
        {
            await _manager.CancelAsync(id).ConfigureAwait(false);
            return new ConfirmVendorReturnResultAppDto { Success = true };
        }
        catch (VendorReturnNotFoundException ex)
        {
            return new ConfirmVendorReturnResultAppDto { Success = false, ErrorMessage = ex.Message };
        }
        catch (ReturnCannotChangeStatusException ex)
        {
            return new ConfirmVendorReturnResultAppDto { Success = false, ErrorMessage = ex.Message };
        }
        catch (Exception ex)
        {
            return new ConfirmVendorReturnResultAppDto { Success = false, ErrorMessage = ex.Message };
        }
    }

    public async Task<ConfirmVendorReturnResultAppDto> ReverseConfirmedAsync(Guid id, string reason)
    {
        try
        {
            await _manager.ReverseConfirmedAsync(id, reason).ConfigureAwait(false);
            return new ConfirmVendorReturnResultAppDto { Success = true };
        }
        catch (Exception ex)
        {
            return new ConfirmVendorReturnResultAppDto { Success = false, ErrorMessage = ex.Message };
        }
    }

    public async Task<VendorReturnAppDto?> GetByIdAsync(Guid id)
    {
        var dto = await _manager.GetByIdAsync(id).ConfigureAwait(false);
        return dto?.ToAppDto();
    }

    public async Task<(int Total, List<VendorReturnAppDto> Items)> GetListAsync(
        Guid? vendorId, Guid? purchaseOrderId, Guid? goodsReceiptId, int? status, int pageIndex, int pageSize)
    {
        var (total, items) = await _manager.GetListAsync(
            vendorId, purchaseOrderId, goodsReceiptId, status, pageIndex, pageSize).ConfigureAwait(false);

        return (total, items.Select(i => i.ToAppDto()).ToList());
    }

    public Task<List<GoodsReceiptPickerAppDto>> GetGoodsReceiptsByVendorAsync(Guid vendorId, Guid? purchaseOrderId = null)
    {
        var query = goodsReceiptDataReader.DataSource
            .Where(gr => gr.VendorId == vendorId
                         && gr.SourceType == GoodsReceiptSourceType.FromVendor);

        if (purchaseOrderId.HasValue)
            query = query.Where(gr => gr.PurchaseOrderId == purchaseOrderId.Value);

        var receipts = query
            .OrderByDescending(gr => gr.ReceivedOnUtc)
            .ToList();

        if (receipts.Count == 0)
            return Task.FromResult(new List<GoodsReceiptPickerAppDto>());

        // Batch load warehouse names referenced by GR items
        var warehouseIds = receipts
            .SelectMany(r => r.Items)
            .Select(i => i.WarehouseId)
            .OfType<Guid>()
            .Distinct()
            .ToList();
        var warehouseNameDict = warehouseDataReader.DataSource
            .Where(w => warehouseIds.Contains(w.Id))
            .ToDictionary(w => w.Id, w => w.Name);

        // Batch load confirmed returns linked to these GRs → tính total đã trả per (GR, Product)
        var grIds = receipts.Select(r => r.Id).ToList();
        var returnedByGrProduct = vendorReturnDataReader.DataSource
            .Where(r => r.GoodsReceiptId.HasValue
                        && grIds.Contains(r.GoodsReceiptId!.Value)
                        && (int)r.Status == 2) // Confirmed
            .ToList()
            .SelectMany(r => r.Items.Select(i => new
            {
                GrId = r.GoodsReceiptId!.Value,
                i.ProductId,
                i.AcceptedQuantity
            }))
            .GroupBy(x => new { x.GrId, x.ProductId })
            .ToDictionary(g => (g.Key.GrId, g.Key.ProductId), g => g.Sum(x => x.AcceptedQuantity));

        var result = receipts.Select(gr =>
        {
            var warehouseIds = gr.Items
                .Select(i => i.WarehouseId)
                .OfType<Guid>()
                .Distinct()
                .ToList();
            var warehouseNames = warehouseIds
                .Select(id => warehouseNameDict.TryGetValue(id, out var name) ? name : null)
                .OfType<string>()
                .ToList();

            var totalQty = gr.Items.Sum(i => i.Quantity);
            var totalValue = gr.Items.Sum(i => i.Quantity * (i.UnitCost ?? 0));
            var isPendingCosting = gr.Items.Any(i => !i.UnitCost.HasValue);

            // IsFullyReturned: với mọi product trong GR, tổng đã trả >= tổng đã nhập của product đó trong GR này
            var receivedPerProduct = gr.Items
                .GroupBy(i => i.ProductId)
                .ToDictionary(g => g.Key, g => g.Sum(i => i.Quantity));
            var isFullyReturned = receivedPerProduct.Count > 0
                && receivedPerProduct.All(kv =>
                    returnedByGrProduct.TryGetValue((gr.Id, kv.Key), out var returned)
                    && returned >= kv.Value);

            return new GoodsReceiptPickerAppDto(gr.Id)
            {
                Code = gr.Code,
                Label = gr.PurchaseOrderCode is not null
                    ? $"{gr.Code} · PO {gr.PurchaseOrderCode} · {gr.ReceivedOnUtc:dd/MM/yyyy}"
                    : $"{gr.Code} · {gr.ReceivedOnUtc:dd/MM/yyyy}",
                ReceivedOnUtc = gr.ReceivedOnUtc,
                PurchaseOrderCode = gr.PurchaseOrderCode,
                WarehouseIds = warehouseIds,
                WarehouseNames = warehouseNames,
                ItemCount = gr.Items.Count,
                TotalQuantity = totalQty,
                TotalValue = totalValue,
                IsPendingCosting = isPendingCosting,
                IsFullyReturned = isFullyReturned
            };
        }).ToList();

        return Task.FromResult(result);
    }

    public Task<List<Guid>> GetWarehousesWithSufficientStockAsync(
        IReadOnlyList<(Guid ProductId, decimal RequiredQty)> items)
    {
        ArgumentNullException.ThrowIfNull(items);

        if (items.Count == 0)
            return Task.FromResult(warehouseDataReader.DataSource.Select(w => w.Id).ToList());

        // Sum required qty per product (cùng product có thể xuất hiện nhiều dòng)
        var requiredByProduct = items
            .Where(t => t.RequiredQty > 0)
            .GroupBy(t => t.ProductId)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.RequiredQty));

        if (requiredByProduct.Count == 0)
            return Task.FromResult(warehouseDataReader.DataSource.Select(w => w.Id).ToList());

        var productIds = requiredByProduct.Keys.ToList();

        // Load stocks for these products, group by warehouse
        var stocks = inventoryStockDataReader.DataSource
            .Where(s => productIds.Contains(s.ProductId))
            .ToList();

        var availableByWarehouseProduct = stocks
            .GroupBy(s => s.WarehouseId)
            .ToDictionary(
                g => g.Key,
                g => g.GroupBy(s => s.ProductId)
                      .ToDictionary(pg => pg.Key, pg => pg.Sum(s => s.QuantityAvailable)));

        // A warehouse is valid if it has >= required qty for ALL products
        var validIds = new List<Guid>();
        foreach (var (warehouseId, productAvailable) in availableByWarehouseProduct)
        {
            var isValid = requiredByProduct.All(kv =>
                productAvailable.TryGetValue(kv.Key, out var avail) && avail >= kv.Value);

            if (isValid)
                validIds.Add(warehouseId);
        }

        return Task.FromResult(validIds);
    }

    public Task<List<ReturnableItemAppDto>> GetGoodsReceiptItemsForReturnAsync(
        Guid goodsReceiptId, Guid? excludeReturnId = null)
    {
        var goodsReceipt = goodsReceiptDataReader.DataSource
            .FirstOrDefault(gr => gr.Id == goodsReceiptId);
        if (goodsReceipt is null)
            return Task.FromResult(new List<ReturnableItemAppDto>());

        // Tính số lượng đã trả theo từng ProductId
        var confirmedReturns = vendorReturnDataReader.DataSource
            .Where(r => r.GoodsReceiptId == goodsReceiptId
                        && (int)r.Status == 2 // Confirmed
                        && (excludeReturnId == null || r.Id != excludeReturnId))
            .ToList();

        // Batch load products
        var productIds = goodsReceipt.Items.Select(i => i.ProductId).Distinct().ToList();
        var products = productDataReader.DataSource
            .Where(p => productIds.Contains(p.Id))
            .ToList();
        var productDict = products.ToDictionary(p => p.Id);

        // Batch load units
        var unitIds = products
            .Where(p => p.UnitMeasurementId.HasValue)
            .Select(p => p.UnitMeasurementId!.Value)
            .Distinct()
            .ToList();
        var unitMeasurements = unitMeasurementDataReader.DataSource
            .Where(u => unitIds.Contains(u.Id))
            .ToList();
        var unitDict = unitMeasurements.ToDictionary(u => u.Id, u => u.Name);
        var unitDecimalPlacesDict = unitMeasurements.ToDictionary(u => u.Id, u => u.DecimalPlaces);

        var result = goodsReceipt.Items.Select(item =>
        {
            var alreadyReturned = confirmedReturns
                .SelectMany(r => r.Items.Where(i => i.ProductId == item.ProductId))
                .Sum(i => i.AcceptedQuantity);

            productDict.TryGetValue(item.ProductId, out var product);
            var productName = product?.Name ?? $"[{item.ProductId}]";
            var unit = "";
            var decimalPlaces = 0;
            if (product?.UnitMeasurementId.HasValue == true)
            {
                unitDict.TryGetValue(product.UnitMeasurementId.Value, out unit);
                unitDecimalPlacesDict.TryGetValue(product.UnitMeasurementId.Value, out decimalPlaces);
            }

            return new ReturnableItemAppDto
            {
                ProductId = item.ProductId,
                ProductName = productName,
                Unit = unit ?? "",
                OriginalQty = item.Quantity,
                AlreadyReturnedQty = alreadyReturned,
                UnitPrice = item.UnitCost ?? 0,
                SourceItemId = item.Id,
                QuantityDecimalPlaces = decimalPlaces
            };
        }).ToList();

        return Task.FromResult(result);
    }
}
