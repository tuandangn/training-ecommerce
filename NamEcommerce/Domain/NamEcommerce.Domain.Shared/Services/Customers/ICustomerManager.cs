using NamEcommerce.Domain.Shared.Dtos.Common;
using NamEcommerce.Domain.Shared.Dtos.Customers;

namespace NamEcommerce.Domain.Shared.Services.Customers;

public interface ICustomerManager
{
    Task<CustomerDto?> GetCustomerByIdAsync(Guid id);
    Task<IPagedDataDto<CustomerDto>> GetCustomersAsync(int pageIndex, int pageSize, string? keywords = null, bool includeSystem = false, int? kind = null);

    Task<CreateCustomerResultDto> CreateCustomerAsync(CreateCustomerDto dto);
    Task<UpdateCustomerResultDto> UpdateCustomerAsync(UpdateCustomerDto dto);
    Task ChangeCustomerKindAsync(ChangeCustomerKindDto dto);
    Task DeleteCustomerAsync(Guid id);

    Task<CustomerDto> GetOrCreateRetailWalkInCustomerAsync();
    Task<bool> IsRetailWalkInCustomerAsync(Guid customerId);
}
