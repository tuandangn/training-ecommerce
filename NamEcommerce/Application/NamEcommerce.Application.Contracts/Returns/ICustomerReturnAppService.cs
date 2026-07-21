using NamEcommerce.Application.Contracts.Dtos.Returns;

namespace NamEcommerce.Application.Contracts.Returns;

public interface ICustomerReturnAppService
{
    Task<CustomerReturnAppDto?> GetByIdAsync(Guid id);
    Task<CreateCustomerReturnResultAppDto> CreateAsync(CreateCustomerReturnAppDto dto);
    Task<UpdateCustomerReturnResultAppDto> UpdateAsync(UpdateCustomerReturnAppDto dto);

    Task<ConfirmCustomerReturnResultAppDto> ConfirmAsync(Guid id, Guid? warehouseId = null);
    Task<ConfirmCustomerReturnResultAppDto> CancelAsync(Guid id);
    Task<ConfirmCustomerReturnResultAppDto> MoveToInspectingAsync(Guid id);

    Task<(int Total, List<CustomerReturnAppDto> Items)> GetListAsync(
        int pageIndex, int pageSize, Guid? customerId = null, Guid? deliveryNoteId = null, int? status = null);

    Task<List<DeliveryNotePickerAppDto>> GetDeliveryNotesByCustomerAsync(Guid customerId);

    Task<List<ReturnableItemAppDto>> GetDeliveryNoteItemsForReturnAsync(Guid deliveryNoteId, Guid? excludeReturnId = null);

    Task<List<ReturnableItemAppDto>> GetReturnableItemsByCustomerAsync(Guid customerId, Guid? excludeReturnId = null);
}
