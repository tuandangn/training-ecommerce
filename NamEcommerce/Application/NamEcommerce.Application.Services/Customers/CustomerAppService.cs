using NamEcommerce.Application.Contracts.Dtos.Common;
using NamEcommerce.Domain.Shared.Services.Customers;
using NamEcommerce.Application.Contracts.Dtos.Customers;
using NamEcommerce.Application.Contracts.Customers;
using NamEcommerce.Domain.Shared.Dtos.Customers;
using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Domain.Entities.Customers;
using NamEcommerce.Domain.Shared.Enums.Customers;
using NamEcommerce.Application.Contracts.Users;

namespace NamEcommerce.Application.Services.Customers;

public sealed class CustomerAppService(ICustomerManager customerManager, IEntityDataReader<Customer> customerDataReader, ICurrentUserService currentUserService) : ICustomerAppService
{
    public async Task<CreateCustomerResultAppDto> CreateCustomerAsync(CreateCustomerAppDto dto)
    {
        var isAdmin = await currentUserService.IsAdminAsync().ConfigureAwait(false);
        var result = await customerManager.CreateCustomerAsync(new CreateCustomerDto
        {
            FullName = dto.FullName,
            PhoneNumber = dto.PhoneNumber,
            Email = dto.Email,
            Address = dto.Address,
            Note = dto.Note,
            Kind = isAdmin ? (CustomerKind) dto.Kind : CustomerKind.RetailWalkIn
        }).ConfigureAwait(false);

        return new CreateCustomerResultAppDto
        {
            Success = true,
            CreatedId = result.CreatedId
        };
    }

    public async Task<UpdateCustomerResultAppDto> UpdateCustomerAsync(UpdateCustomerAppDto dto)
    {
        var customer = await customerDataReader.GetByIdAsync(dto.Id).ConfigureAwait(false);
        if (customer is null)
            return new UpdateCustomerResultAppDto { Success = false, ErrorMessage = "Error.CustomerIsNotFound" };

        var isAdmin = await currentUserService.IsAdminAsync().ConfigureAwait(false);
        var result = await customerManager.UpdateCustomerAsync(new UpdateCustomerDto(dto.Id)
        {
            FullName = dto.FullName,
            PhoneNumber = dto.PhoneNumber,
            Kind = isAdmin ? (CustomerKind) dto.Kind : customer.Kind,
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
        await customerManager.DeleteCustomerAsync(id).ConfigureAwait(false);

        return new DeleteCustomerResultAppDto
        {
            Success = true,
            DeletedId = id
        };
    }

    public async Task<CustomerAppDto?> GetCustomerByIdAsync(Guid id)
    {
        var dto = await customerDataReader.GetByIdAsync(id).ConfigureAwait(false);
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
        var dto = await customerManager.GetOrCreateRetailWalkInCustomerAsync().ConfigureAwait(false);
        return MapToAppDto(dto);
    }

    public async Task<IEnumerable<CustomerAppDto>> GetCustomersByIdsAsync(IEnumerable<Guid> ids)
    {
        var customers = await customerDataReader.GetByIdsAsync(ids).ConfigureAwait(false);

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

    public async Task<IPagedDataAppDto<CustomerAppDto>> GetCustomersAsync(int pageIndex, int pageSize, string? keywords = null, int? kind = null, bool includeSystem = false)
    {
        var paged = await customerManager.GetCustomersAsync(pageIndex, pageSize, keywords, includeSystem, kind).ConfigureAwait(false);
        var items = paged.Items.Where(c => !c.IsSystem).Select(MapToAppDto).ToList();

        foreach(var systemCustomer in paged.Items.Where(c => c.IsSystem).OrderByDescending(c => c.FullName))
        {
            items.Insert(0, MapToAppDto(systemCustomer));
        }

        return PagedDataAppDto.Create(items, pageIndex, pageSize, paged.PagerInfo.TotalCount);

        CustomerAppDto MapToAppDto(CustomerDto dto)
            => new(dto.Id)
            {
                FullName = dto.FullName,
                PhoneNumber = dto.PhoneNumber,
                Email = dto.Email,
                Address = dto.Address,
                Note = dto.Note,
                Kind = (int) dto.Kind,
                IsSystem = dto.IsSystem,
                CreatedOnUtc = dto.CreatedOnUtc
            };
    }

    private static CustomerAppDto MapToAppDto(CustomerDto dto)
        => new(dto.Id)
        {
            FullName = dto.FullName,
            PhoneNumber = dto.PhoneNumber,
            Email = dto.Email,
            Address = dto.Address,
            Note = dto.Note,
            Kind = (int) dto.Kind,
            IsSystem = dto.IsSystem,
            CreatedOnUtc = dto.CreatedOnUtc
        };
}
