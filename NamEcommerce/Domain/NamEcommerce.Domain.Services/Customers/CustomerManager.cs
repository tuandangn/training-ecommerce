using NamEcommerce.Data.Contracts;
using NamEcommerce.Domain.Entities.Orders;
using NamEcommerce.Domain.Shared.Dtos.Common;
using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Domain.Entities.Customers;
using NamEcommerce.Domain.Shared.Enums.Customers;
using NamEcommerce.Domain.Shared.Exceptions.Customers;
using NamEcommerce.Domain.Shared.Services.Customers;
using NamEcommerce.Domain.Shared.Dtos.Customers;
using Microsoft.EntityFrameworkCore;
using NamEcommerce.Domain.Shared.Specifications;
using NamEcommerce.Domain.Specifications.Customers;

namespace NamEcommerce.Domain.Services.Customers;

public sealed class CustomerManager(
    IRepository<Customer> customerRepository,
    IEntityDataReader<Customer> customerDataReader,
    IEntityDataReader<Order> orderDataReader) : ICustomerManager
{
    public async Task<CreateCustomerResultDto> CreateCustomerAsync(CreateCustomerDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        var customer = new Customer(Guid.NewGuid(), dto.FullName, dto.PhoneNumber, dto.Address)
        {
            Email = dto.Email,
            Note = dto.Note
        };
        customer.SetCustomerKind(dto.Kind);
        customer.ClearDomainEvents();
        customer.MarkCreated();
        var inserted = await customerRepository.InsertAsync(customer).ConfigureAwait(false);
        return new CreateCustomerResultDto { CreatedId = inserted.Id };
    }

    public async Task<UpdateCustomerResultDto> UpdateCustomerAsync(UpdateCustomerDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        var customer = await customerRepository.GetByIdAsync(dto.Id).ConfigureAwait(false);
        if (customer == null) throw new CustomerIsNotFoundException(dto.Id);
        if (customer.IsSystem)
            throw new CustomerCannotBeUpdatedException();

        customer.FullName = dto.FullName;
        customer.PhoneNumber = dto.PhoneNumber;
        customer.Email = dto.Email;
        customer.Address = dto.Address;
        customer.Note = dto.Note;
        customer.SetCustomerKind(dto.Kind);
        customer.MarkUpdated();

        await customerRepository.UpdateAsync(customer).ConfigureAwait(false);
        return new UpdateCustomerResultDto { UpdatedId = customer.Id };
    }

    public async Task DeleteCustomerAsync(Guid id)
    {
        var customer = await customerRepository.GetByIdAsync(id).ConfigureAwait(false);
        if (customer != null)
        {
            if (customer.IsSystem)
                throw new CustomerCannotBeDeletedException(id);

            var hasOrders = await orderDataReader.DataSource.AnyAsync(o => o.CustomerId == id).ConfigureAwait(false);
            if (hasOrders) throw new CustomerCannotBeDeletedException(id);

            customer.MarkDeleted();
            await customerRepository.DeleteAsync(customer).ConfigureAwait(false);
        }
    }

    public async Task<bool> IsRetailWalkInCustomerAsync(Guid customerId)
    {
        var customer = await customerRepository.GetByIdAsync(customerId).ConfigureAwait(false);

        return customer is { Kind: CustomerKind.RetailWalkIn };
    }

    public async Task ChangeCustomerKindAsync(ChangeCustomerKindDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var customer = await customerDataReader.GetByIdAsync(dto.CustomerId).ConfigureAwait(false);
        if (customer is null)
            throw new CustomerIsNotFoundException(dto.CustomerId);
        if (customer.IsSystem)
            throw new CustomerCannotChangeKindException();

        customer.SetCustomerKind(dto.Kind);
        await customerRepository.UpdateAsync(customer).ConfigureAwait(false);
    }

    public async Task<CustomerDto?> GetCustomerByIdAsync(Guid id)
    {
        var customer = await customerRepository.GetByIdAsync(id).ConfigureAwait(false);
        if (customer == null) return null;

        return new CustomerDto(customer.Id)
        {
            FullName = customer.FullName,
            PhoneNumber = customer.PhoneNumber,
            Email = customer.Email,
            Address = customer.Address,
            Note = customer.Note,
            Kind = customer.Kind,
            IsSystem = customer.IsSystem,
            CreatedOnUtc = customer.CreatedOnUtc
        };
    }

    public async Task<CustomerDto> GetOrCreateRetailWalkInCustomerAsync()
    {
        var existing = await customerDataReader.DataSource
            .FirstOrDefaultAsync(c => c.Kind == CustomerKind.RetailWalkIn && c.IsSystem)
            .ConfigureAwait(false);
        if (existing is not null)
            return MapToDto(existing);

        var customer = new Customer(Guid.NewGuid(), CustomerConsts.RETAIL_WALKIN_CUSTOMER_NAME, CustomerConsts.RETAIL_WALKIN_CUSTOMER_PHONE, CustomerConsts.RETAIL_WALKIN_CUSTOMER_ADDRESS)
        {
            IsSystem = true
        };
        customer.SetCustomerKind(CustomerKind.RetailWalkIn);
        customer.MarkCreated();

        var inserted = await customerRepository.InsertAsync(customer).ConfigureAwait(false);
        return MapToDto(inserted);
    }

    public async Task<IPagedDataDto<CustomerDto>> GetCustomersAsync(int pageIndex, int pageSize, string? keywords = null, bool includeSystem = false, int? kind = null)
    {
        var customerSpec = new CompositeSpecification<Customer>();

        if (!includeSystem)
            customerSpec.And(new IsSystemCustomerSpec(false));

        if (kind.HasValue)
            customerSpec.And(new HaveKindCustomerSpec((CustomerKind)kind));

        if (!string.IsNullOrWhiteSpace(keywords))
            customerSpec.And(new CustomerKeywordSearchSpec(keywords));

        int? totalCount = pageIndex == 0 && pageSize == int.MaxValue
            ? null : await customerDataReader.CountAsync(customerSpec).ConfigureAwait(false);

        if (totalCount.HasValue && totalCount == 0)
            return PagedDataDto.Create(new List<CustomerDto>(), pageIndex, pageSize, 0);

        customerSpec.ApplyOrderBy(c => c.FullName.Value);
        var pagedData = await customerDataReader.GetPagedListAsync(customerSpec, pageIndex, pageSize).ConfigureAwait(false);

        return PagedDataDto.Create(pagedData.Select(c => new CustomerDto(c.Id)
        {
            FullName = c.FullName,
            PhoneNumber = c.PhoneNumber,
            Email = c.Email,
            Address = c.Address,
            Note = c.Note,
            Kind = c.Kind,
            IsSystem = c.IsSystem,
            CreatedOnUtc = c.CreatedOnUtc
        }), pageIndex, pageSize, totalCount);
    }

    private static CustomerDto MapToDto(Customer customer)
        => new(customer.Id)
        {
            FullName = customer.FullName,
            PhoneNumber = customer.PhoneNumber,
            Email = customer.Email,
            Address = customer.Address,
            Note = customer.Note,
            Kind = customer.Kind,
            IsSystem = customer.IsSystem,
            CreatedOnUtc = customer.CreatedOnUtc
        };
}
