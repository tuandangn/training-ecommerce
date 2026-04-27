using NamEcommerce.Application.Contracts.Dtos.Common;
using NamEcommerce.Application.Contracts.Dtos.GoodsReceipts;
using NamEcommerce.Application.Contracts.GoodsReceipts;
using NamEcommerce.Application.Services.Extensions;
using NamEcommerce.Domain.Entities.Catalog;
using NamEcommerce.Domain.Entities.Inventory;
using NamEcommerce.Domain.Entities.Media;
using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Domain.Shared.Dtos.GoodsReceipts;
using NamEcommerce.Domain.Shared.Services.GoodsReceipts;

namespace NamEcommerce.Application.Services.GoodsReceipts;

public sealed class GoodsReceiptAppService : IGoodsReceiptAppService
{
    private readonly IGoodsReceiptManager _goodsReceiptManager;
    private readonly IEntityDataReader<Product> _productDataReader;
    private readonly IEntityDataReader<Warehouse> _warehouseDataReader;
    private readonly IEntityDataReader<Picture> _pictureDataReader;

    public GoodsReceiptAppService(
        IGoodsReceiptManager goodsReceiptManager,
        IEntityDataReader<Product> productDataReader,
        IEntityDataReader<Warehouse> warehouseDataReader,
        IEntityDataReader<Picture> pictureDataReader)
    {
        _goodsReceiptManager = goodsReceiptManager;
        _productDataReader = productDataReader;
        _warehouseDataReader = warehouseDataReader;
        _pictureDataReader = pictureDataReader;
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
                return new CreateGoodsReceiptResultAppDto
                {
                    Success = false,
                    ErrorMessage = "Error.GoodsReceipt.ProductIsNotFound"
                };

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
            }).ToList()
        };
        var result = await _goodsReceiptManager.CreateGoodsReceiptAsync(createDto).ConfigureAwait(false);

        return new CreateGoodsReceiptResultAppDto
        {
            Success = true,
            CreatedId = result.CreatedId
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

        var deleteDto = new DeleteGoodsReceiptDto(id)
        {
            ReceivedOnUtc = goodsReceipt.ReceivedOnUtc,
            PictureIds = goodsReceipt.PictureIds
        };

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

    public async Task<CommonActionResultDto> SetGoodsReceiptToPurchaseOrder(SetGoodsReceiptToPurchaseOrderAppDto dto)
    {
        try
        {
            await _goodsReceiptManager.SetGoodsReceiptToPurchaseOrder(new SetGoodsReceiptToPurchaseOrderDto(dto.Id, dto.PurchaseOrderId)).ConfigureAwait(false);
            return CommonActionResultDto.CreateSuccess();
        }
        catch (Exception ex)
        {
            return CommonActionResultDto.CreateError(ex.Message);
        }
    }

    public async Task<CommonActionResultDto> RemoveGoodsReceiptFromPurchaseOrder(RemoveGoodsReceiptFromPurchaseOrderAppDto dto)
    {
        try
        {
            await _goodsReceiptManager.RemoveGoodsReceiptFromPurchaseOrder(new RemoveGoodsReceiptFromPurchaseOrderDto(dto.Id)).ConfigureAwait(false);
            return CommonActionResultDto.CreateSuccess();
        }
        catch (Exception ex)
        {
            return CommonActionResultDto.CreateError(ex.Message);
        }
    }
}
