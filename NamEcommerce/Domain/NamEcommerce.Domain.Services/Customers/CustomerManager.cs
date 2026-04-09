using NamEcommerce.Data.Contracts;
using NamEcommerce.Domain.Entities.Orders;
using NamEcommerce.Domain.Shared.Dtos.Common;
using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Domain.Entities.Customers;
using NamEcommerce.Domain.Shared.Services.Customers;
using NamEcommerce.Domain.Shared.Helpers;
using NamEcommerce.Domain.Shared.Dtos.Customers;

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
        var inserted = await _customerRepository.InsertAsync(customer).ConfigureAwait(false);
        return new CreateCustomerResultDto { CreatedId = inserted.Id };
    }

    public async Task<UpdateCustomerResultDto> UpdateCustomerAsync(UpdateCustomerDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        var customer = await _customerDataReader.GetByIdAsync(dto.Id).ConfigureAwait(false);
        if (customer == null) throw new ArgumentException("Customer not found");

        customer.FullName = dto.FullName;
        customer.PhoneNumber = dto.PhoneNumber;
        customer.Email = dto.Email;
        customer.Address = dto.Address;
        customer.Note = dto.Note;

        await _customerRepository.UpdateAsync(customer).ConfigureAwait(false);
        return new UpdateCustomerResultDto { UpdatedId = customer.Id };
    }

    public async Task DeleteCustomerAsync(Guid id)
    {
        var hasOrders = await Task.Run(() => _orderDataReader.DataSource.Any(o => o.CustomerId == id)).ConfigureAwait(false);
        if (hasOrders) throw new InvalidOperationException("Cannot delete customer with existing orders");

        var customer = await _customerDataReader.GetByIdAsync(id).ConfigureAwait(false);
        if (customer != null)
        {
            await _customerRepository.DeleteAsync(customer).ConfigureAwait(false);
        }
    }

    public async Task<CustomerDto?> GetCustomerByIdAsync(Guid id)
    {
        var customer = await _customerDataReader.GetByIdAsync(id).ConfigureAwait(false);
        if (customer == null) return null;

        return new CustomerDto(customer.Id)
        {
            FullName = customer.FullName,
            PhoneNumber = customer.PhoneNumber,
            Email = customer.Email,
            Address = customer.Address,
            Note = customer.Note,
            CreatedOnUtc = customer.CreatedOnUtc
        };
    }

    public async Task<IPagedDataDto<CustomerDto>> GetCustomersAsync(string? keywords, int pageIndex, int pageSize)
    {
        var query = _customerDataReader.DataSource;

        if (!string.IsNullOrWhiteSpace(keywords))
        {
            var normalizedKeywords = TextHelper.Normalize(keywords);
            query = query.Where(c =>
                c.FullName.Contains(keywords) || c.FullName.Contains(normalizedKeywords) || c.NormalizedFullName.Contains(normalizedKeywords)
                || c.Address.Contains(keywords) || c.Address.Contains(normalizedKeywords) || c.NormalizedAddress.Contains(normalizedKeywords)
                || c.PhoneNumber.Contains(keywords));
        }

        var total = query.Count();
        var paged = query.OrderByDescending(c => c.CreatedOnUtc)
                        .Skip(pageIndex * pageSize)
                        .Take(pageSize)
                        .ToList()
                        .Select(c => new CustomerDto(c.Id)
                        {
                            FullName = c.FullName,
                            PhoneNumber = c.PhoneNumber,
                            Email = c.Email,
                            Address = c.Address,
                            Note = c.Note,
                            CreatedOnUtc = c.CreatedOnUtc
                        }).ToList();

        return PagedDataDto.Create(paged, pageIndex, pageSize, total);
    }
}
