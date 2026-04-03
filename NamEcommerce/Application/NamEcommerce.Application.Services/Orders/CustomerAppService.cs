using NamEcommerce.Application.Contracts.Orders;
using NamEcommerce.Application.Contracts.Dtos.Orders;
using NamEcommerce.Domain.Shared.Services.Orders;
using NamEcommerce.Application.Contracts.Dtos.Common;

namespace NamEcommerce.Application.Services.Orders;

public sealed class CustomerAppService : ICustomerAppService
{
    private readonly ICustomerManager _customerManager;

    public CustomerAppService(ICustomerManager customerManager)
    {
        _customerManager = customerManager;
    }

    public async Task<CreateCustomerResultAppDto> CreateCustomerAsync(CreateCustomerAppDto dto)
    {
        var result = await _customerManager.CreateCustomerAsync(new NamEcommerce.Domain.Shared.Dtos.Orders.CreateCustomerDto
        {
            FullName = dto.FullName,
            PhoneNumber = dto.PhoneNumber,
            Email = dto.Email,
            Address = dto.Address,
            Note = dto.Note
        }).ConfigureAwait(false);

        return new CreateCustomerResultAppDto { Success = true, CreatedId = result.CreatedId };
    }

    public async Task<UpdateCustomerResultAppDto> UpdateCustomerAsync(UpdateCustomerAppDto dto)
    {
        var result = await _customerManager.UpdateCustomerAsync(new NamEcommerce.Domain.Shared.Dtos.Orders.UpdateCustomerDto(dto.Id)
        {
            FullName = dto.FullName,
            PhoneNumber = dto.PhoneNumber,
            Email = dto.Email,
            Address = dto.Address,
            Note = dto.Note
        }).ConfigureAwait(false);

        return new UpdateCustomerResultAppDto { Success = true, UpdatedId = result.UpdatedId };
    }

    public async Task<DeleteCustomerResultAppDto> DeleteCustomerAsync(Guid id)
    {
        try
        {
            await _customerManager.DeleteCustomerAsync(id).ConfigureAwait(false);
            return new DeleteCustomerResultAppDto { Success = true, DeletedId = id };
        }
        catch (Exception ex)
        {
            return new DeleteCustomerResultAppDto { Success = false, ErrorMessage = ex.Message };
        }
    }

    public async Task<CustomerAppDto?> GetCustomerByIdAsync(Guid id)
    {
        var dto = await _customerManager.GetCustomerByIdAsync(id).ConfigureAwait(false);
        if (dto == null) return null;

        return new CustomerAppDto(dto.Id)
        {
            FullName = dto.FullName,
            PhoneNumber = dto.PhoneNumber,
            Email = dto.Email,
            Address = dto.Address,
            Note = dto.Note,
            CreatedOnUtc = dto.CreatedOnUtc
        };
    }

    public async Task<IPagedDataAppDto<CustomerAppDto>> GetCustomersAsync(string? keywords, int pageIndex, int pageSize)
    {
        var paged = await _customerManager.GetCustomersAsync(keywords, pageIndex, pageSize).ConfigureAwait(false);
        var items = paged.Items.Select(c => new CustomerAppDto(c.Id)
        {
            FullName = c.FullName,
            PhoneNumber = c.PhoneNumber,
            Email = c.Email,
            Address = c.Address,
            Note = c.Note,
            CreatedOnUtc = c.CreatedOnUtc
        }).ToList();

        return PagedDataAppDto.Create(items, pageIndex, pageSize, paged.PagerInfo.TotalCount);
    }
}
