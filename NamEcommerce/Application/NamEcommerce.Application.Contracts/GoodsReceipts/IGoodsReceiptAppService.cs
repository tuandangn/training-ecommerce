using NamEcommerce.Application.Contracts.Dtos.Common;
using NamEcommerce.Application.Contracts.Dtos.GoodsReceipts;

namespace NamEcommerce.Application.Contracts.GoodsReceipts;

public interface IGoodsReceiptAppService
{
    Task<GoodsReceiptAppDto?> GetGoodsReceiptByIdAsync(Guid id);
    Task<IPagedDataAppDto<GoodsReceiptAppDto>> GetGoodsReceiptsAsync(int pageIndex, int pageSize, string? keywords, DateTime? fromDateUtc, DateTime? toDateUtc);

    Task<CreateGoodsReceiptResultAppDto> CreateGoodsReceiptAsync(CreateGoodsReceiptAppDto dto);
    Task<UpdateGoodsReceiptResultAppDto> UpdateGoodsReceiptAsync(UpdateGoodsReceiptAppDto dto);
    Task<(bool success, string? errorMessage)> DeleteGoodsReceiptAsync(Guid id);

    Task<SetGoodsReceiptItemUnitCostResultAppDto> SetGoodsReceiptItemUnitCostAsync(SetGoodsReceiptItemUnitCostAppDto dto);
}
