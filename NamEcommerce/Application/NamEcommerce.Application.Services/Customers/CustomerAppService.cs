using NamEcommerce.Application.Contracts.Dtos.Common;
using NamEcommerce.Domain.Shared.Services.Customers;
using NamEcommerce.Application.Contracts.Dtos.Customers;
using NamEcommerce.Application.Contracts.Customers;
using NamEcommerce.Domain.Shared.Dtos.Customers;
using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Domain.Entities.Customers;

namespace NamEcommerce.Application.Services.Customers;

public sealed class CustomerAppService : ICustomerAppService
{
    private readonly ICustomerManager _customerManager;
    private readonly IEntityDataReader<Customer> _customerDataReader;

    public CustomerAppService(ICustomerManager customerManager, IEntityDataReader<Customer> customerDataReader)
    {
        _customerManager = customerManager;
        _customerDataReader = customerDataReader;
    }

    public async Task<CreateCustomerResultAppDto> CreateCustomerAsync(CreateCustomerAppDto dto)
    {
        var result = await _customerManager.CreateCustomerAsync(new CreateCustomerDto
        {
            FullName = dto.FullName,
            PhoneNumber = dto.PhoneNumber,
            Email = dto.Email,
            Address = dto.Address,
            Note = dto.Note
        }).ConfigureAwait(false);

        return new CreateCustomerResultAppDto
        {
            Success = true,
            CreatedId = result.CreatedId
        };
    }

    public async Task<UpdateCustomerResultAppDto> UpdateCustomerAsync(UpdateCustomerAppDto dto)
    {
        var result = await _customerManager.UpdateCustomerAsync(new UpdateCustomerDto(dto.Id)
        {
            FullName = dto.FullName,
            PhoneNumber = dto.PhoneNumber,
            Email = dto.Email,
            Address = dto.Address,
            Note = dto.Note
        }).ConfigureAwait(false);

        return new UpdateCustomerResultAppDto
        {
            Success = true,
            UpdatedId = result.UpdatedId
        };
    }

    public async Task<DeleteCustomerResultAppDto> DeleteCustomerAsync(Guid id)
    {
        await _customerManager.DeleteCustomerAsync(id).ConfigureAwait(false);

        return new DeleteCustomerResultAppDto
        {
            Success = true,
            DeletedId = id
        };
    }

    public async Task<CustomerAppDto?> GetCustomerByIdAsync(Guid id)
    {
        var dto = await _customerDataReader.GetByIdAsync(id).ConfigureAwait(false);
        if (dto == null) return null;

        return new CustomerAppDto(dto.Id)
        {
            FullName = dto.FullName,
            PhoneNumber = dto.PhoneNumber,
            Email = dto.Email,
            Address = dto.Address,
            Note = dto.Note,
            Kind = (int)dto.Kind,
            IsSystem = dto.IsSystem,
            CreatedOnUtc = dto.CreatedOnUtc
        };
    }

    public async Task<CustomerAppDto> GetOrCreateRetailWalkInCustomerAsync()
    {
        var dto = await _customerManager.GetOrCreateRetailWalkInCustomerAsync().ConfigureAwait(false);
        return MapToAppDto(dto);
    }


    public async Task<IEnumerable<CustomerAppDto>> GetCustomersByIdsAsync(IEnumerable<Guid> ids)
    {
        var customers = await _customerDataReader.GetByIdsAsync(ids).ConfigureAwait(false);

        return customers.Select(cust => new CustomerAppDto(cust.Id)
        {
            FullName = cust.FullName,
            PhoneNumber = cust.PhoneNumber,
            Email = cust.Email,
            Address = cust.Address,
            Note = cust.Note,
            Kind = (int)cust.Kind,
            IsSystem = cust.IsSystem,
            CreatedOnUtc = cust.CreatedOnUtc
        });
    }

    public async Task<IPagedDataAppDto<CustomerAppDto>> GetCustomersAsync(
        string? keywords,
        int pageIndex,
        int pageSize,
        bool includeSystem = false)
    {
        var paged = await _customerManager.GetCustomersAsync(keywords, pageIndex, pageSize, includeSystem).ConfigureAwait(false);
        var items = paged.Items.Select(c => new CustomerAppDto(c.Id)
        {
            FullName = c.FullName,
            PhoneNumber = c.PhoneNumber,
            Email = c.Email,
            Address = c.Address,
            Note = c.Note,
            Kind = (int)c.Kind,
            IsSystem = c.IsSystem,
            CreatedOnUtc = c.CreatedOnUtc
        }).ToList();

        return PagedDataAppDto.Create(items, pageIndex, pageSize, paged.PagerInfo.TotalCount);
    }

    private static CustomerAppDto MapToAppDto(CustomerDto dto)
        => new(dto.Id)
        {
            FullName = dto.FullName,
            PhoneNumber = dto.PhoneNumber,
            Email = dto.Email,
            Address = dto.Address,
            Note = dto.Note,
            Kind = dto.Kind,
            IsSystem = dto.IsSystem,
            CreatedOnUtc = dto.CreatedOnUtc
        };
}
