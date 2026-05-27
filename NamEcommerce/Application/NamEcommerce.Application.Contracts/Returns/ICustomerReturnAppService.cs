using NamEcommerce.Application.Contracts.Dtos.Returns;

namespace NamEcommerce.Application.Contracts.Returns;

public interface ICustomerReturnAppService
{
    Task<CreateCustomerReturnResultAppDto> CreateAsync(CreateCustomerReturnAppDto dto);
    Task<UpdateCustomerReturnResultAppDto> UpdateAsync(UpdateCustomerReturnAppDto dto);
    Task<ConfirmCustomerReturnResultAppDto> MoveToInspectingAsync(Guid id);
    Task<ConfirmCustomerReturnResultAppDto> ConfirmAsync(Guid id);
    Task<ConfirmCustomerReturnResultAppDto> CancelAsync(Guid id);

    Task<CustomerReturnAppDto?> GetByIdAsync(Guid id);
    Task<(int Total, List<CustomerReturnAppDto> Items)> GetListAsync(
        Guid? customerId, Guid? deliveryNoteId, int? status, int pageIndex, int pageSize);

    /// <summary>Lấy danh sách phiếu xuất kho (Delivered, ToCustomer) của một khách hàng — cho AJAX picker.</summary>
    Task<List<DeliveryNotePickerAppDto>> GetDeliveryNotesByCustomerAsync(Guid customerId);

    /// <summary>Lấy danh sách items có thể trả của một phiếu xuất kho — bao gồm số lượng đã trả.</summary>
    Task<List<ReturnableItemAppDto>> GetDeliveryNoteItemsForReturnAsync(Guid deliveryNoteId, Guid? excludeReturnId = null);

    /// <summary>Lấy danh sách hàng đã giao còn có thể trả của một khách hàng, không cần chọn phiếu giao.</summary>
    Task<List<ReturnableItemAppDto>> GetReturnableItemsByCustomerAsync(Guid customerId, Guid? excludeReturnId = null);
}
