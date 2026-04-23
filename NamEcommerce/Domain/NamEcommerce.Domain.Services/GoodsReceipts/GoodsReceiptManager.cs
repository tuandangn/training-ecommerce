using NamEcommerce.Data.Contracts;
using NamEcommerce.Domain.Entities.Catalog;
using NamEcommerce.Domain.Entities.GoodsReceipts;
using NamEcommerce.Domain.Entities.Inventory;
using NamEcommerce.Domain.Entities.Media;
using NamEcommerce.Domain.Services.Extensions;
using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Domain.Shared.Dtos.Common;
using NamEcommerce.Domain.Shared.Dtos.GoodsReceipts;
using NamEcommerce.Domain.Shared.Events;
using NamEcommerce.Domain.Shared.Exceptions.GoodsReceipts;
using NamEcommerce.Domain.Shared.Helpers;
using NamEcommerce.Domain.Shared.Services.GoodsReceipts;
using NamEcommerce.Domain.Shared.Services.Users;
using NamEcommerce.Domain.Shared.Settings;

namespace NamEcommerce.Domain.Services.GoodsReceipts;

public sealed class GoodsReceiptManager(
    IRepository<GoodsReceipt> goodsReceiptRepository,
    IEntityDataReader<GoodsReceipt> goodsReceiptDataReader,
    IEntityDataReader<Product> productDataReader,
    WarehouseSettings warehouseSettings,
    IEntityDataReader<Warehouse> warehouseDataReader,
    ICurrentUserAccessor currentUserAccessor,
    IEntityDataReader<Picture> pictureDataReader,
    IEventPublisher eventPublisher) : IGoodsReceiptManager
{
    public async Task<CreateGoodsReceiptResultDto> CreateGoodsReceiptAsync(CreateGoodsReceiptDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        dto.Verify();

        var goodsReceipt = await GoodsReceipt.CreateAsync(Guid.NewGuid(), goodsReceiptDataReader, currentUserAccessor);
        goodsReceipt.SetCreationDate(dto.CreatedOnUtc);
        goodsReceipt.TruckDriverName = dto.TruckDriverName;
        goodsReceipt.TruckNumberSerial = dto.TruckNumberSerial;
        foreach (var item in dto.Items)
            await goodsReceipt.AddItemAsync(item.ProductId, item.WarehouseId, item.Quantity, item.UnitCost, productDataReader, warehouseSettings, warehouseDataReader).ConfigureAwait(false);
        foreach (var pictureId in dto.PictureIds)
            await goodsReceipt.AddPictureAsync(pictureId, pictureDataReader).ConfigureAwait(false);

        var insertedGoodsReceipt = await goodsReceiptRepository.InsertAsync(goodsReceipt).ConfigureAwait(false);

        await eventPublisher.EntityCreated(insertedGoodsReceipt).ConfigureAwait(false);

        return new CreateGoodsReceiptResultDto { CreatedId = insertedGoodsReceipt.Id };
    }

    public async Task<UpdateGoodsReceiptResultDto> UpdateGoodsReceiptAsync(UpdateGoodsReceiptDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        dto.Verify();

        var goodsReceipt = await goodsReceiptDataReader.GetByIdAsync(dto.Id).ConfigureAwait(false);
        if (goodsReceipt is null)
            throw new GoodsReceiptIsNotFoundException(dto.Id);

        goodsReceipt.SetCreationDate(dto.CreatedOnUtc);
        goodsReceipt.TruckDriverName = dto.TruckDriverName;
        goodsReceipt.TruckNumberSerial = dto.TruckNumberSerial;
        var deletedPictureIds = goodsReceipt.PictureIds.AsEnumerable();
        goodsReceipt.ClearPictures();
        foreach (var pictureId in dto.PictureIds)
            await goodsReceipt.AddPictureAsync(pictureId, pictureDataReader).ConfigureAwait(false);

        var updatedGoodsReceipt = await goodsReceiptRepository.UpdateAsync(goodsReceipt).ConfigureAwait(false);

        await eventPublisher.EntityUpdated(updatedGoodsReceipt, deletedPictureIds).ConfigureAwait(false);

        return new UpdateGoodsReceiptResultDto { UpdatedId = updatedGoodsReceipt.Id };
    }

    public async Task SetGoodsReceiptItemUnitCostAsync(SetGoodsReceiptItemUnitCostDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        dto.Verify();

        var goodsReceipt = await goodsReceiptDataReader.GetByIdAsync(dto.GoodsReceiptId).ConfigureAwait(false);
        if (goodsReceipt is null)
            throw new GoodsReceiptIsNotFoundException(dto.GoodsReceiptId);

        var item = goodsReceipt.Items.FirstOrDefault(item => item.Id == dto.GoodsReceiptItemId);
        if (item is null)
            throw new GoodsReceiptItemIsNotFoundException(dto.GoodsReceiptItemId);

        item.SetUnitCost(dto.UnitCost);
        var updatedGoodsReceipt = await goodsReceiptRepository.UpdateAsync(goodsReceipt).ConfigureAwait(false);

        await eventPublisher.EntityUpdated(updatedGoodsReceipt).ConfigureAwait(false);
    }

    public async Task<GoodsReceiptDto?> GetGoodsReceiptByIdAsync(Guid id)
    {
        var goodsReceipt = await goodsReceiptDataReader.GetByIdAsync(id).ConfigureAwait(false);

        if (goodsReceipt is null)
            return null;
        return goodsReceipt.ToDto();
    }

    public async Task<IPagedDataDto<GoodsReceiptDto>> GetGoodsReceiptsAsync(int pageIndex, int pageSize, string? keywords, DateTime? fromDateUtc, DateTime? toDateUtc)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(pageIndex, 0, nameof(pageIndex));
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(pageSize, 0, nameof(pageSize));

        var query = goodsReceiptDataReader.DataSource;

        if (!string.IsNullOrEmpty(keywords))
        {
            var normalizedKeywords = TextHelper.Normalize(keywords);
            var uppercaseKeywords = keywords.Trim().ToUpper();

            IList<Guid?> userIds = [];

            query = query.Where(goodsReceipt =>
                (goodsReceipt.TruckDriverName != null && (goodsReceipt.TruckDriverName.ToUpper().Contains(uppercaseKeywords) || goodsReceipt.TruckDriverName.ToUpper().Contains(normalizedKeywords) || goodsReceipt.TruckDriverNameNormalized.Contains(normalizedKeywords)))
                || (goodsReceipt.TruckNumberSerial != null && goodsReceipt.TruckNumberSerial.ToUpper().Contains(uppercaseKeywords))
                || userIds.Contains(goodsReceipt.CreatedByUserId));
        }

        if (fromDateUtc.HasValue)
            query = query.Where(goodsReceipt => goodsReceipt.CreatedOnUtc >= fromDateUtc);

        if (toDateUtc.HasValue)
            query = query.Where(goodsReceipt => goodsReceipt.CreatedOnUtc <= toDateUtc);

        var total = query.Count();
        var data = query
            .OrderByDescending(o => o.CreatedOnUtc)
            .Skip(pageIndex * pageSize).Take(pageSize)
            .ToList();

        var pagedData = PagedDataDto.Create(data.Select(goodsReceipt => goodsReceipt.ToDto()), pageIndex, pageSize, total);
        return pagedData;
    }

    public async Task DeleteGoodsReceiptAsync(DeleteGoodsReceiptDto dto)
    {
        var goodsReceipt = await goodsReceiptDataReader.GetByIdAsync(dto.GoodsReceiptId).ConfigureAwait(false);
        if (goodsReceipt is null)
            throw new GoodsReceiptIsNotFoundException(dto.GoodsReceiptId);

        await goodsReceiptRepository.DeleteAsync(goodsReceipt).ConfigureAwait(false);

        await eventPublisher.EntityDeleted(goodsReceipt).ConfigureAwait(false);
    }
}
