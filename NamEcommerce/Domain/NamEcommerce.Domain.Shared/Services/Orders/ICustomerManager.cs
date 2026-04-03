using NamEcommerce.Domain.Shared.Dtos.Orders;
using NamEcommerce.Domain.Shared.Dtos.Common;

namespace NamEcommerce.Domain.Shared.Services.Orders;

public interface ICustomerManager
{
    Task<CreateCustomerResultDto> CreateCustomerAsync(CreateCustomerDto dto);
    Task<UpdateCustomerResultDto> UpdateCustomerAsync(UpdateCustomerDto dto);
    Task DeleteCustomerAsync(Guid id);
    Task<CustomerDto?> GetCustomerByIdAsync(Guid id);
    Task<IPagedDataDto<CustomerDto>> GetCustomersAsync(string? keywords, int pageIndex, int pageSize);
}
