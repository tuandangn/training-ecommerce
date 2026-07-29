using NamEcommerce.Data.Contracts;
using NamEcommerce.Domain.Entities.Orders;
using NamEcommerce.Domain.Shared.Dtos.Common;
using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Domain.Entities.Customers;
using NamEcommerce.Domain.Shared.Enums.Customers;
using NamEcommerce.Domain.Shared.Exceptions;
using NamEcommerce.Domain.Shared.Exceptions.Customers;
using NamEcommerce.Domain.Shared.Services.Customers;
using NamEcommerce.Domain.Shared.Helpers;
using NamEcommerce.Domain.Shared.Dtos.Customers;
using Microsoft.EntityFrameworkCore;

namespace NamEcommerce.Domain.Services.Customers;

public sealed class CustomerManager : ICustomerManager
{
    private readonly IRepository<Customer> _customerRepository;
    private readonly IEntityDataReader<Customer> _customerDataReader;
    private readonly IEntityDataReader<Order> _orderDataReader;

    public CustomerManager(
        IRepository<Customer> customerRepository,
        IEntityDataReader<Customer> customerDataReader,
        IEntityDataReader<Order> orderDataReader)
    {
        _customerRepository = customerRepository;
        _customerDataReader = customerDataReader;
        _orderDataReader = orderDataReader;
    }

    public async Task<CreateCustomerResultDto> CreateCustomerAsync(CreateCustomerDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        var customer = new Customer(Guid.NewGuid(), dto.FullName, dto.PhoneNumber, dto.Address)
        {
            Email = dto.Email,
            Note = dto.Note
        };
        customer.MarkCreated();
        var inserted = await _customerRepository.InsertAsync(customer).ConfigureAwait(false);
        return new CreateCustomerResultDto { CreatedId = inserted.Id };
    }

    public async Task<UpdateCustomerResultDto> UpdateCustomerAsync(UpdateCustomerDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        var customer = await _customerRepository.GetByIdAsync(dto.Id).ConfigureAwait(false);
        if (customer == null) throw new CustomerIsNotFoundException(dto.Id);
        if (customer.IsSystem)
            throw new NamEcommerceDomainException("Error.SystemCustomerCannotBeUpdated");

        customer.FullName = dto.FullName;
        customer.PhoneNumber = dto.PhoneNumber;
        customer.Email = dto.Email;
        customer.Address = dto.Address;
        customer.Note = dto.Note;
        customer.MarkUpdated();

        await _customerRepository.UpdateAsync(customer).ConfigureAwait(false);
        return new UpdateCustomerResultDto { UpdatedId = customer.Id };
    }

    public async Task DeleteCustomerAsync(Guid id)
    {
        var customer = await _customerRepository.GetByIdAsync(id).ConfigureAwait(false);
        if (customer != null)
        {
            if (customer.IsSystem)
                throw new CustomerCannotBeDeletedException(id);

            var hasOrders = await _orderDataReader.DataSource.AnyAsync(o => o.CustomerId == id).ConfigureAwait(false);
            if (hasOrders) throw new CustomerCannotBeDeletedException(id);

            customer.MarkDeleted();
            await _customerRepository.DeleteAsync(customer).ConfigureAwait(false);
        }
    }

    public async Task<CustomerDto?> GetCustomerByIdAsync(Guid id)
    {
        var customer = await _customerRepository.GetByIdAsync(id).ConfigureAwait(false);
        if (customer == null) return null;

        return new CustomerDto(customer.Id)
        {
            FullName = customer.FullName,
            PhoneNumber = customer.PhoneNumber,
            Email = customer.Email,
            Address = customer.Address,
            Note = customer.Note,
            Kind = (int)customer.Kind,
            IsSystem = customer.IsSystem,
            CreatedOnUtc = customer.CreatedOnUtc
        };
    }

    public async Task<CustomerDto> GetOrCreateRetailWalkInCustomerAsync()
    {
        var existing = await _customerDataReader.DataSource
            .FirstOrDefaultAsync(c => c.Kind == CustomerKind.RetailWalkIn && c.IsSystem)
            .ConfigureAwait(false);
        if (existing is not null)
            return MapToDto(existing);

        var customer = new Customer(Guid.NewGuid(), CustomerConsts.RETAIL_WALKIN_CUSTOMER_NAME, CustomerConsts.RETAIL_WALKIN_CUSTOMER_PHONE, CustomerConsts.RETAIL_WALKIN_CUSTOMER_ADDRESS)
        {
            Kind = CustomerKind.RetailWalkIn,
            IsSystem = true
        };
        customer.MarkCreated();

        var inserted = await _customerRepository.InsertAsync(customer).ConfigureAwait(false);
        return MapToDto(inserted);
    }

    public async Task<IPagedDataDto<CustomerDto>> GetCustomersAsync(int pageIndex, int pageSize, string? keywords = null, bool includeSystem = false)
    {
        var query = _customerDataReader.DataSource;

        if (!includeSystem)
            query = query.Where(c => !c.IsSystem);

        if (!string.IsNullOrWhiteSpace(keywords))
        {
            var normalizedKeywords = TextHelper.Normalize(keywords);
            var uppercaseKeywords = keywords.Trim().ToUpper();
            query = query.Where(c =>
                c.FullName.Value.ToUpper().Contains(uppercaseKeywords) || c.FullName.Value.ToUpper().Contains(normalizedKeywords) || c.FullName.NormalizedValue.Contains(normalizedKeywords)
                || c.Address.Value.ToUpper().Contains(uppercaseKeywords) || c.Address.Value.ToUpper().Contains(normalizedKeywords) || c.Address.NormalizedValue.Contains(normalizedKeywords)
                || c.PhoneNumber.Contains(keywords));
        }

        var total = await query.CountAsync().ConfigureAwait(false);
        var paged = await query
            .OrderBy(c => c.FullName.Value)
            .ThenByDescending(c => c.CreatedOnUtc)
            .Skip(pageIndex * pageSize).Take(pageSize)
            .Select(c => new CustomerDto(c.Id)
            {
                FullName = c.FullName,
                PhoneNumber = c.PhoneNumber,
                Email = c.Email,
                Address = c.Address,
                Note = c.Note,
                Kind = (int)c.Kind,
                IsSystem = c.IsSystem,
                CreatedOnUtc = c.CreatedOnUtc
            }).ToListAsync().ConfigureAwait(false);

        return PagedDataDto.Create(paged, pageIndex, pageSize, total);
    }

    private static CustomerDto MapToDto(Customer customer)
        => new(customer.Id)
        {
            FullName = customer.FullName,
            PhoneNumber = customer.PhoneNumber,
            Email = customer.Email,
            Address = customer.Address,
            Note = customer.Note,
            Kind = (int)customer.Kind,
            IsSystem = customer.IsSystem,
            CreatedOnUtc = customer.CreatedOnUtc
        };

    public async Task<bool> IsRetailWalkInCustomerAsync(Guid customerId)
    {
        var customer = await _customerRepository.GetByIdAsync(customerId).ConfigureAwait(false);

        return customer is { Kind: CustomerKind.RetailWalkIn };
    }
}
