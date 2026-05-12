using NamEcommerce.Application.Contracts.Dtos.Returns;
using NamEcommerce.Application.Contracts.Returns;
using NamEcommerce.Application.Services.Extensions;
using NamEcommerce.Domain.Entities.Catalog;
using NamEcommerce.Domain.Entities.GoodsReceipts;
using NamEcommerce.Domain.Entities.Returns;
using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Domain.Shared.Dtos.Returns;
using NamEcommerce.Domain.Shared.Enums.GoodsReceipts;
using NamEcommerce.Domain.Shared.Exceptions.Returns;
using NamEcommerce.Domain.Shared.Services.Returns;

namespace NamEcommerce.Application.Services.Returns;

public sealed class VendorReturnAppService(
    IVendorReturnManager manager,
    IEntityDataReader<GoodsReceipt> goodsReceiptDataReader,
    IEntityDataReader<VendorReturn> vendorReturnDataReader,
    IEntityDataReader<Product> productDataReader,
    IEntityDataReader<UnitMeasurement> unitMeasurementDataReader) : IVendorReturnAppService
{
    private readonly IVendorReturnManager _manager = manager;

    public async Task<CreateVendorReturnResultAppDto> CreateAsync(
        CreateVendorReturnAppDto dto, Guid? createdByUserId)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var (valid, errorMessage) = dto.Validate();
        if (!valid)
            return new CreateVendorReturnResultAppDto { Success = false, ErrorMessage = errorMessage };

        try
        {
            var domainDto = new CreateVendorReturnDto
            {
                VendorId = dto.VendorId,
                PurchaseOrderId = dto.PurchaseOrderId,
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

            var result = await _manager.CreateAsync(domainDto, createdByUserId).ConfigureAwait(false);
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

    public async Task<ConfirmVendorReturnResultAppDto> ConfirmAsync(Guid id)
    {
        try
        {
            await _manager.ConfirmAsync(id).ConfigureAwait(false);
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

    public Task<List<GoodsReceiptPickerAppDto>> GetGoodsReceiptsByVendorAsync(Guid vendorId)
    {
        var receipts = goodsReceiptDataReader.DataSource
            .Where(gr => gr.VendorId == vendorId
                         && gr.SourceType == GoodsReceiptSourceType.FromVendor)
            .OrderByDescending(gr => gr.ReceivedOnUtc)
            .ToList();

        var result = receipts.Select(gr => new GoodsReceiptPickerAppDto(gr.Id)
        {
            Label = gr.PurchaseOrderCode is not null
                ? $"PO: {gr.PurchaseOrderCode} — {gr.ReceivedOnUtc:dd/MM/yyyy}"
                : $"Nhập {gr.ReceivedOnUtc:dd/MM/yyyy}",
            ReceivedOnUtc = gr.ReceivedOnUtc,
            PurchaseOrderCode = gr.PurchaseOrderCode
        }).ToList();

        return Task.FromResult(result);
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
        var unitDict = unitMeasurementDataReader.DataSource
            .Where(u => unitIds.Contains(u.Id))
            .ToDictionary(u => u.Id, u => u.Name);

        var result = goodsReceipt.Items.Select(item =>
        {
            var alreadyReturned = confirmedReturns
                .SelectMany(r => r.Items.Where(i => i.ProductId == item.ProductId))
                .Sum(i => i.AcceptedQuantity);

            productDict.TryGetValue(item.ProductId, out var product);
            var productName = product?.Name ?? $"[{item.ProductId}]";
            var unit = "";
            if (product?.UnitMeasurementId.HasValue == true)
                unitDict.TryGetValue(product.UnitMeasurementId.Value, out unit);

            return new ReturnableItemAppDto
            {
                ProductId = item.ProductId,
                ProductName = productName,
                Unit = unit ?? "",
                OriginalQty = item.Quantity,
                AlreadyReturnedQty = alreadyReturned,
                UnitPrice = item.UnitCost ?? 0,
                SourceItemId = item.Id
            };
        }).ToList();

        return Task.FromResult(result);
    }
}
