using NamEcommerce.Application.Contracts.Dtos.Returns;

namespace NamEcommerce.Application.Contracts.Returns;

public interface IVendorReturnAppService
{
    Task<CreateVendorReturnResultAppDto> CreateAsync(CreateVendorReturnAppDto dto, Guid? createdByUserId);
    Task<UpdateVendorReturnResultAppDto> UpdateAsync(UpdateVendorReturnAppDto dto);
    Task<ConfirmVendorReturnResultAppDto> MoveToInspectingAsync(Guid id);
    Task<ConfirmVendorReturnResultAppDto> ConfirmAsync(Guid id);
    Task<ConfirmVendorReturnResultAppDto> CancelAsync(Guid id);

    Task<VendorReturnAppDto?> GetByIdAsync(Guid id);
    Task<(int Total, List<VendorReturnAppDto> Items)> GetListAsync(
        Guid? vendorId, Guid? purchaseOrderId, Guid? goodsReceiptId, int? status, int pageIndex, int pageSize);
}
