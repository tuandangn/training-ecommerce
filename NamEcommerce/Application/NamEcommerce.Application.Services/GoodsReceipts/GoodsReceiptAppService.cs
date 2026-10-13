using NamEcommerce.Application.Contracts.Dtos.Common;
using NamEcommerce.Application.Contracts.Dtos.GoodsReceipts;
using NamEcommerce.Application.Contracts.GoodsReceipts;
using NamEcommerce.Application.Contracts.PurchaseOrders;
using NamEcommerce.Application.Services.Extensions;
using NamEcommerce.Domain.Entities.Catalog;
using NamEcommerce.Domain.Entities.Inventory;
using NamEcommerce.Domain.Entities.Media;
using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Domain.Shared.Dtos.GoodsReceipts;
using NamEcommerce.Domain.Shared.Helpers;
using NamEcommerce.Domain.Shared.Services.GoodsReceipts;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace NamEcommerce.Application.Services.GoodsReceipts;

public sealed class GoodsReceiptAppService : IGoodsReceiptAppService
{
    private readonly IGoodsReceiptManager _goodsReceiptManager;
    private readonly IPurchaseOrderAppService _purchaseOrderAppService;
    private readonly IEntityDataReader<Product> _productDataReader;
    private readonly IEntityDataReader<Warehouse> _warehouseDataReader;
    private readonly IEntityDataReader<Picture> _pictureDataReader;
    private readonly IEntityDataReader<UnitMeasurement> _unitMeasurementDataReader;

    public GoodsReceiptAppService(
        IGoodsReceiptManager goodsReceiptManager,
        IPurchaseOrderAppService purchaseOrderAppService,
        IEntityDataReader<Product> productDataReader,
        IEntityDataReader<Warehouse> warehouseDataReader,
        IEntityDataReader<Picture> pictureDataReader,
        IEntityDataReader<UnitMeasurement> unitMeasurementDataReader)
    {
        _goodsReceiptManager = goodsReceiptManager;
        _purchaseOrderAppService = purchaseOrderAppService;
        _productDataReader = productDataReader;
        _warehouseDataReader = warehouseDataReader;
        _pictureDataReader = pictureDataReader;
        _unitMeasurementDataReader = unitMeasurementDataReader;
    }

    public async Task<IPagedDataAppDto<GoodsReceiptAppDto>> GetGoodsReceiptsAsync(
        int pageIndex, int pageSize, string? keywords, DateTime? fromDateUtc, DateTime? toDateUtc)
    {
        var pagedData = await _goodsReceiptManager
            .GetGoodsReceiptsAsync(pageIndex, pageSize, keywords, fromDateUtc, toDateUtc)
            .ConfigureAwait(false);

        return PagedDataAppDto.Create(
            pagedData.Items.Select(item => item.ToDto()),
            pageIndex, pageSize,
            pagedData.PagerInfo.TotalCount);
    }

    public async Task<GoodsReceiptAppDto?> GetGoodsReceiptByIdAsync(Guid id)
    {
        var goodsReceipt = await _goodsReceiptManager.GetGoodsReceiptByIdAsync(id).ConfigureAwait(false);
        if (goodsReceipt is null)
            return null;

        return goodsReceipt.ToDto();
    }

    public async Task<IList<GoodsReceiptAppDto>> GetGoodsReceiptsByPurchaseOrderIdAsync(Guid purchaseOrderId)
    {
        var receipts = await _goodsReceiptManager.GetGoodsReceiptsByPurchaseOrderIdAsync(purchaseOrderId).ConfigureAwait(false);
        return receipts.Select(r => r.ToDto()).ToList();
    }

    public async Task<CreateGoodsReceiptResultAppDto> CreateGoodsReceiptAsync(CreateGoodsReceiptAppDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var (valid, errorMessage) = dto.Validate();
        if (!valid)
            return new CreateGoodsReceiptResultAppDto { Success = false, ErrorMessage = errorMessage };

        // Kiểm tra từng sản phẩm và kho trong danh sách item
        foreach (var item in dto.Items)
        {
            var product = await _productDataReader.GetByIdAsync(item.ProductId).ConfigureAwait(false);
            if (product is null)
            {
                return new CreateGoodsReceiptResultAppDto
                {
                    Success = false,
                    ErrorMessage = "Error.GoodsReceipt.ProductIsNotFound"
                };
            }

            if (product.UnitMeasurementId.HasValue)
            {
                var unitMeasurement = await _unitMeasurementDataReader.GetByIdAsync(product.UnitMeasurementId.Value).ConfigureAwait(false);
                if (unitMeasurement is not null)
                {
                    if (!NumberHelper.IsValidDecimalPlace(item.Quantity, unitMeasurement.DecimalPlaces))
                    {
                        return new CreateGoodsReceiptResultAppDto
                        {
                            Success = false,
                            ErrorMessage = "Error.QuantityMustBeInteger"
                        };
                    }
                }
            }

            if (item.WarehouseId.HasValue)
            {
                var warehouse = await _warehouseDataReader.GetByIdAsync(item.WarehouseId.Value).ConfigureAwait(false);
                if (warehouse is null)
                    return new CreateGoodsReceiptResultAppDto
                    {
                        Success = false,
                        ErrorMessage = "Error.GoodsReceipt.WarehouseIsNotFound"
                    };
            }
        }

        // Kiểm tra ảnh chứng từ tồn tại
        foreach (var pictureId in dto.PictureIds)
        {
            var picture = await _pictureDataReader.GetByIdAsync(pictureId).ConfigureAwait(false);
            if (picture is null)
                return new CreateGoodsReceiptResultAppDto
                {
                    Success = false,
                    ErrorMessage = "Error.GoodsReceipt.PictureIsNotFound"
                };
        }

        var createDto = new CreateGoodsReceiptDto
        {
            ReceivedOnUtc = dto.ReceivedOnUtc,
            TruckDriverName = dto.TruckDriverName,
            TruckNumberSerial = dto.TruckNumberSerial,
            PictureIds = dto.PictureIds,
            Note = dto.Note,
            VendorId = dto.VendorId,
            Items = dto.Items.Select(item => new AddGoodsReceiptItemDto
            {
                ProductId = item.ProductId,
                WarehouseId = item.WarehouseId,
                Quantity = item.Quantity,
                UnitCost = item.UnitCost
            }).ToList(),
            TaxRate = dto.TaxRate
        };
        var result = await _goodsReceiptManager.CreateGoodsReceiptAsync(createDto).ConfigureAwait(false);

        return new CreateGoodsReceiptResultAppDto
        {
            Success = true,
            CreatedId = result.CreatedId,
        };
    }

    public async Task<UpdateGoodsReceiptResultAppDto> UpdateGoodsReceiptAsync(UpdateGoodsReceiptAppDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var (valid, errorMessage) = dto.Validate();
        if (!valid)
            return new UpdateGoodsReceiptResultAppDto { Success = false, ErrorMessage = errorMessage };

        // Kiểm tra phiếu nhập tồn tại
        var goodsReceipt = await _goodsReceiptManager.GetGoodsReceiptByIdAsync(dto.Id).ConfigureAwait(false);
        if (goodsReceipt is null)
        {
            return new UpdateGoodsReceiptResultAppDto
            {
                Success = false,
                ErrorMessage = "Error.GoodsReceipt.IsNotFound"
            };
        }

        // Kiểm tra ảnh chứng từ tồn tại
        foreach (var pictureId in dto.PictureIds)
        {
            var picture = await _pictureDataReader.GetByIdAsync(pictureId).ConfigureAwait(false);
            if (picture is null)
                return new UpdateGoodsReceiptResultAppDto
                {
                    Success = false,
                    ErrorMessage = "Error.GoodsReceipt.PictureIsNotFound"
                };
        }

        var updateDto = new UpdateGoodsReceiptDto(dto.Id)
        {
            ReceivedOnUtc = dto.ReceivedOnUtc,
            TruckDriverName = dto.TruckDriverName,
            TruckNumberSerial = dto.TruckNumberSerial,
            PictureIds = dto.PictureIds,
            Note = dto.Note,
            VendorId = dto.VendorId
        };

        var result = await _goodsReceiptManager.UpdateGoodsReceiptAsync(updateDto).ConfigureAwait(false);

        return new UpdateGoodsReceiptResultAppDto
        {
            Success = true,
            UpdatedId = result.UpdatedId
        };
    }

    public async Task<(bool success, string? errorMessage)> DeleteGoodsReceiptAsync(Guid id)
    {
        var goodsReceipt = await _goodsReceiptManager.GetGoodsReceiptByIdAsync(id).ConfigureAwait(false);
        if (goodsReceipt is null)
            return (false, "Error.GoodsReceipt.IsNotFound");

        //if (goodsReceipt.PurchaseOrderId.HasValue)
        //    return (false, "Error.GoodsReceipt.CannotDeleteWhenHasPurchaseOrder");

        var deleteDto = new DeleteGoodsReceiptDto(id);
        await _goodsReceiptManager.DeleteGoodsReceiptAsync(deleteDto).ConfigureAwait(false);

        return (true, null);
    }

    public async Task<SetGoodsReceiptItemUnitCostResultAppDto> SetGoodsReceiptItemUnitCostAsync(
        SetGoodsReceiptItemUnitCostAppDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var (valid, errorMessage) = dto.Validate();
        if (!valid)
            return new SetGoodsReceiptItemUnitCostResultAppDto { Success = false, ErrorMessage = errorMessage };

        // Kiểm tra phiếu nhập tồn tại
        var goodsReceipt = await _goodsReceiptManager.GetGoodsReceiptByIdAsync(dto.GoodsReceiptId).ConfigureAwait(false);
        if (goodsReceipt is null)
            return new SetGoodsReceiptItemUnitCostResultAppDto
            {
                Success = false,
                ErrorMessage = "Error.GoodsReceipt.IsNotFound"
            };

        // Kiểm tra item tồn tại trong phiếu
        var item = goodsReceipt.Items.FirstOrDefault(i => i.Id == dto.GoodsReceiptItemId);
        if (item is null)
            return new SetGoodsReceiptItemUnitCostResultAppDto
            {
                Success = false,
                ErrorMessage = "Error.GoodsReceipt.ItemIsNotFound"
            };

        // Chỉ cho phép cập nhật giá khi item chưa có đơn giá (UnitCost = null → pending costing)
        if (item.UnitCost.HasValue)
            return new SetGoodsReceiptItemUnitCostResultAppDto
            {
                Success = false,
                ErrorMessage = "Error.GoodsReceipt.ItemUnitCostAlreadySet"
            };

        var domainDto = new SetGoodsReceiptItemUnitCostDto
        {
            GoodsReceiptId = dto.GoodsReceiptId,
            GoodsReceiptItemId = dto.GoodsReceiptItemId,
            UnitCost = dto.UnitCost
        };

        await _goodsReceiptManager.SetGoodsReceiptItemUnitCostAsync(domainDto).ConfigureAwait(false);

        return new SetGoodsReceiptItemUnitCostResultAppDto { Success = true };
    }

    public async Task<SetGoodsReceiptVendorResultAppDto> SetGoodsReceiptVendorAsync(SetGoodsReceiptVendorAppDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var (valid, errorMessage) = dto.Validate();
        if (!valid)
            return new SetGoodsReceiptVendorResultAppDto { Success = false, ErrorMessage = errorMessage };

        var domainDto = new SetGoodsReceiptVendorDto(dto.GoodsReceiptId)
        {
            VendorId = dto.VendorId
        };

        var result = await _goodsReceiptManager.SetGoodsReceiptVendorAsync(domainDto).ConfigureAwait(false);

        return new SetGoodsReceiptVendorResultAppDto
        {
            Success = true,
            UpdatedId = result.UpdatedId
        };
    }

    public Task<CommonActionResultDto> SetGoodsReceiptToPurchaseOrder(SetGoodsReceiptToPurchaseOrderAppDto dto)
        => _purchaseOrderAppService.SetGoodsReceiptToPurchaseOrderAsync(dto);

    public Task<CommonActionResultDto> RemoveGoodsReceiptFromPurchaseOrder(RemoveGoodsReceiptFromPurchaseOrderAppDto dto)
        => _purchaseOrderAppService.RemoveGoodsReceiptFromPurchaseOrderAsync(dto);

    public async Task<IList<SuggestedPurchaseOrderForGoodsReceiptAppDto>> GetSuggestedPurchaseOrdersAsync(Guid goodsReceiptId)
    {
        var domainList = await _goodsReceiptManager.GetSuggestedPurchaseOrdersAsync(goodsReceiptId).ConfigureAwait(false);

        if (domainList.Count == 0)
            return [];

        // Thu thập tất cả ProductId cần enrich tên, batch query một lần.
        var productIds = domainList
            .SelectMany(po => po.Items.Select(i => i.ProductId))
            .Distinct()
            .ToList();

        var productNameMap = new Dictionary<Guid, string?>();
        foreach (var productId in productIds)
        {
            var product = await _productDataReader.GetByIdAsync(productId).ConfigureAwait(false);
            if (product is not null)
                productNameMap[productId] = product.Name;
        }

        return domainList.Select(po => new SuggestedPurchaseOrderForGoodsReceiptAppDto
        {
            PurchaseOrderId = po.PurchaseOrderId,
            PurchaseOrderCode = po.PurchaseOrderCode,
            PlacedOnUtc = po.PlacedOnUtc,
            VendorId = po.VendorId,
            MatchScore = po.MatchScore,
            IsFullMatch = po.IsFullMatch,
            Items = po.Items.Select(i => new SuggestedPurchaseOrderItemForGoodsReceiptAppDto
            {
                ProductId = i.ProductId,
                ProductName = productNameMap.GetValueOrDefault(i.ProductId),
                QuantityOrdered = i.QuantityOrdered,
                QuantityReceived = i.QuantityReceived,
                UnitCost = i.UnitCost
            }).ToList()
        }).ToList();
    }
}
