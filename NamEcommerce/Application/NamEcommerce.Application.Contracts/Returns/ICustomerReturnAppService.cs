using NamEcommerce.Application.Contracts.Dtos.Returns;

namespace NamEcommerce.Application.Contracts.Returns;

public interface ICustomerReturnAppService
{
    Task<CreateCustomerReturnResultAppDto> CreateAsync(CreateCustomerReturnAppDto dto, Guid? createdByUserId);
    Task<UpdateCustomerReturnResultAppDto> UpdateAsync(UpdateCustomerReturnAppDto dto);
    Task<ConfirmCustomerReturnResultAppDto> MoveToInspectingAsync(Guid id);
    Task<ConfirmCustomerReturnResultAppDto> ConfirmAsync(Guid id);
    Task<ConfirmCustomerReturnResultAppDto> CancelAsync(Guid id);

    Task<CustomerReturnAppDto?> GetByIdAsync(Guid id);
    Task<(int Total, List<CustomerReturnAppDto> Items)> GetListAsync(
        Guid? customerId, Guid? orderId, int? status, int pageIndex, int pageSize);
}
