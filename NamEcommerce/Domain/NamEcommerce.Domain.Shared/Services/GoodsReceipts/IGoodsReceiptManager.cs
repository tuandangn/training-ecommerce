using NamEcommerce.Domain.Shared.Dtos.Common;
using NamEcommerce.Domain.Shared.Dtos.GoodsReceipts;

namespace NamEcommerce.Domain.Shared.Services.GoodsReceipts;

public interface IGoodsReceiptManager
{
    Task<CreateGoodsReceiptResultDto> CreateGoodsReceiptAsync(CreateGoodsReceiptDto dto);
    Task<UpdateGoodsReceiptResultDto> UpdateGoodsReceiptAsync(UpdateGoodsReceiptDto dto);
    Task DeleteGoodsReceiptAsync(DeleteGoodsReceiptDto dto);

    Task SetGoodsReceiptItemUnitCostAsync(SetGoodsReceiptItemUnitCostDto dto);
    Task<SetGoodsReceiptVendorResultDto> SetGoodsReceiptVendorAsync(SetGoodsReceiptVendorDto dto);

    Task RemoveGoodsReceiptFromPurchaseOrder(RemoveGoodsReceiptFromPurchaseOrderDto dto);
    Task SetGoodsReceiptToPurchaseOrder(SetGoodsReceiptToPurchaseOrderDto dto);

    Task<GoodsReceiptDto?> GetGoodsReceiptByIdAsync(Guid id);
    Task<IPagedDataDto<GoodsReceiptDto>> GetGoodsReceiptsAsync(int pageIndex, int pageSize, string? keywords, DateTime? fromDateUtc, DateTime? toDateUtc);
}
