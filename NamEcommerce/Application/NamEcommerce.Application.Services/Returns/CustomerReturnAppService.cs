using NamEcommerce.Application.Contracts.Dtos.Returns;
using NamEcommerce.Application.Contracts.Returns;
using NamEcommerce.Application.Services.Extensions;
using NamEcommerce.Domain.Shared.Dtos.Returns;
using NamEcommerce.Domain.Shared.Exceptions.Returns;
using NamEcommerce.Domain.Shared.Services.Returns;

namespace NamEcommerce.Application.Services.Returns;

public sealed class CustomerReturnAppService : ICustomerReturnAppService
{
    private readonly ICustomerReturnManager _manager;

    public CustomerReturnAppService(ICustomerReturnManager manager)
    {
        _manager = manager;
    }

    public async Task<CreateCustomerReturnResultAppDto> CreateAsync(
        CreateCustomerReturnAppDto dto, Guid? createdByUserId)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var (valid, errorMessage) = dto.Validate();
        if (!valid)
            return new CreateCustomerReturnResultAppDto { Success = false, ErrorMessage = errorMessage };

        try
        {
            var domainDto = new CreateCustomerReturnDto
            {
                OrderId = dto.OrderId,
                WarehouseId = dto.WarehouseId,
                Note = dto.Note,
                Items = dto.Items.Select(i => new CreateCustomerReturnItemDto
                {
                    ProductId = i.ProductId,
                    DeliveryNoteItemId = i.DeliveryNoteItemId,
                    RequestedQuantity = i.RequestedQuantity,
                    AcceptedQuantity = i.AcceptedQuantity,
                    UnitPrice = i.UnitPrice
                })
            };

            var result = await _manager.CreateAsync(domainDto, createdByUserId).ConfigureAwait(false);
            return new CreateCustomerReturnResultAppDto { Success = true, CreatedId = result.Id };
        }
        catch (ReturnDataIsInvalidException ex)
        {
            return new CreateCustomerReturnResultAppDto { Success = false, ErrorMessage = ex.Message };
        }
        catch (Exception ex)
        {
            return new CreateCustomerReturnResultAppDto { Success = false, ErrorMessage = ex.Message };
        }
    }

    public async Task<UpdateCustomerReturnResultAppDto> UpdateAsync(UpdateCustomerReturnAppDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        try
        {
            var domainDto = new UpdateCustomerReturnDto(dto.Id)
            {
                Note = dto.Note,
                ReturnDate = dto.ReturnDate
            };

            await _manager.UpdateAsync(domainDto).ConfigureAwait(false);
            return new UpdateCustomerReturnResultAppDto { Success = true };
        }
        catch (CustomerReturnNotFoundException ex)
        {
            return new UpdateCustomerReturnResultAppDto { Success = false, ErrorMessage = ex.Message };
        }
        catch (Exception ex)
        {
            return new UpdateCustomerReturnResultAppDto { Success = false, ErrorMessage = ex.Message };
        }
    }

    public async Task<ConfirmCustomerReturnResultAppDto> MoveToInspectingAsync(Guid id)
    {
        try
        {
            await _manager.MoveToInspectingAsync(id).ConfigureAwait(false);
            return new ConfirmCustomerReturnResultAppDto { Success = true };
        }
        catch (CustomerReturnNotFoundException ex)
        {
            return new ConfirmCustomerReturnResultAppDto { Success = false, ErrorMessage = ex.Message };
        }
        catch (ReturnCannotChangeStatusException ex)
        {
            return new ConfirmCustomerReturnResultAppDto { Success = false, ErrorMessage = ex.Message };
        }
        catch (Exception ex)
        {
            return new ConfirmCustomerReturnResultAppDto { Success = false, ErrorMessage = ex.Message };
        }
    }

    public async Task<ConfirmCustomerReturnResultAppDto> ConfirmAsync(Guid id)
    {
        try
        {
            await _manager.ConfirmAsync(id).ConfigureAwait(false);
            return new ConfirmCustomerReturnResultAppDto { Success = true };
        }
        catch (CustomerReturnNotFoundException ex)
        {
            return new ConfirmCustomerReturnResultAppDto { Success = false, ErrorMessage = ex.Message };
        }
        catch (ReturnCannotChangeStatusException ex)
        {
            return new ConfirmCustomerReturnResultAppDto { Success = false, ErrorMessage = ex.Message };
        }
        catch (ExceedsDeliveredQuantityException ex)
        {
            return new ConfirmCustomerReturnResultAppDto { Success = false, ErrorMessage = ex.Message };
        }
        catch (Exception ex)
        {
            return new ConfirmCustomerReturnResultAppDto { Success = false, ErrorMessage = ex.Message };
        }
    }

    public async Task<ConfirmCustomerReturnResultAppDto> CancelAsync(Guid id)
    {
        try
        {
            await _manager.CancelAsync(id).ConfigureAwait(false);
            return new ConfirmCustomerReturnResultAppDto { Success = true };
        }
        catch (CustomerReturnNotFoundException ex)
        {
            return new ConfirmCustomerReturnResultAppDto { Success = false, ErrorMessage = ex.Message };
        }
        catch (ReturnCannotChangeStatusException ex)
        {
            return new ConfirmCustomerReturnResultAppDto { Success = false, ErrorMessage = ex.Message };
        }
        catch (Exception ex)
        {
            return new ConfirmCustomerReturnResultAppDto { Success = false, ErrorMessage = ex.Message };
        }
    }

    public async Task<CustomerReturnAppDto?> GetByIdAsync(Guid id)
    {
        var dto = await _manager.GetByIdAsync(id).ConfigureAwait(false);
        return dto?.ToAppDto();
    }

    public async Task<(int Total, List<CustomerReturnAppDto> Items)> GetListAsync(
        Guid? customerId, Guid? orderId, int? status, int pageIndex, int pageSize)
    {
        var (total, items) = await _manager.GetListAsync(
            customerId, orderId, status, pageIndex, pageSize).ConfigureAwait(false);

        return (total, items.Select(i => i.ToAppDto()).ToList());
    }
}
