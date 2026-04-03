using NamEcommerce.Application.Contracts.Dtos.Orders;
using NamEcommerce.Application.Contracts.Dtos.Common;

namespace NamEcommerce.Application.Contracts.Orders;

public interface ICustomerAppService
{
    Task<CreateCustomerResultAppDto> CreateCustomerAsync(CreateCustomerAppDto dto);
    Task<UpdateCustomerResultAppDto> UpdateCustomerAsync(UpdateCustomerAppDto dto);
    Task<DeleteCustomerResultAppDto> DeleteCustomerAsync(Guid id);
    Task<CustomerAppDto?> GetCustomerByIdAsync(Guid id);
    Task<IPagedDataAppDto<CustomerAppDto>> GetCustomersAsync(string? keywords, int pageIndex, int pageSize);
}
