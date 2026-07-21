using NamEcommerce.Application.Contracts.Dtos.Common;
using NamEcommerce.Application.Contracts.Dtos.Customers;

namespace NamEcommerce.Application.Contracts.Customers;

public interface ICustomerAppService
{
    Task<CustomerAppDto?> GetCustomerByIdAsync(Guid id);
    Task<IEnumerable<CustomerAppDto>> GetCustomersByIdsAsync(IEnumerable<Guid> ids);
    Task<IPagedDataAppDto<CustomerAppDto>> GetCustomersAsync(int pageIndex, int pageSize, string? keywords = null, bool includeSystem = false);

    Task<CreateCustomerResultAppDto> CreateCustomerAsync(CreateCustomerAppDto dto);
    Task<UpdateCustomerResultAppDto> UpdateCustomerAsync(UpdateCustomerAppDto dto);
    Task<DeleteCustomerResultAppDto> DeleteCustomerAsync(Guid id);

    Task<CustomerAppDto> GetOrCreateRetailWalkInCustomerAsync();
}
