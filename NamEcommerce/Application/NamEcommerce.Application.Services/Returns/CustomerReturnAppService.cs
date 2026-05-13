using NamEcommerce.Application.Contracts.Dtos.Returns;
using NamEcommerce.Application.Contracts.Returns;
using NamEcommerce.Application.Services.Extensions;
using NamEcommerce.Domain.Entities.Catalog;
using NamEcommerce.Domain.Entities.DeliveryNotes;
using NamEcommerce.Domain.Entities.Returns;
using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Domain.Shared.Dtos.Returns;
using NamEcommerce.Domain.Shared.Enums.DeliveryNotes;
using NamEcommerce.Domain.Shared.Exceptions.Returns;
using NamEcommerce.Domain.Shared.Services.Returns;

namespace NamEcommerce.Application.Services.Returns;

public sealed class CustomerReturnAppService(
    ICustomerReturnManager manager,
    IEntityDataReader<DeliveryNote> deliveryNoteDataReader,
    IEntityDataReader<CustomerReturn> customerReturnDataReader,
    IEntityDataReader<Product> productDataReader,
    IEntityDataReader<UnitMeasurement> unitMeasurementDataReader) : ICustomerReturnAppService
{
    private readonly ICustomerReturnManager _manager = manager;

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
                DeliveryNoteId = dto.DeliveryNoteId,
                WarehouseId = dto.WarehouseId,
                Note = dto.Note,
                AdditionalCost = dto.AdditionalCost,
                Items = dto.Items.Select(i => new CreateCustomerReturnItemDto
                {
                    ProductId = i.ProductId,
                    DeliveryNoteItemId = i.DeliveryNoteItemId,
                    RequestedQuantity = i.RequestedQuantity,
                    AcceptedQuantity = i.AcceptedQuantity,
                    OriginalUnitPrice = i.OriginalUnitPrice,
                    ReturnUnitPrice = i.ReturnUnitPrice
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
        Guid? customerId, Guid? deliveryNoteId, int? status, int pageIndex, int pageSize)
    {
        var (total, items) = await _manager.GetListAsync(
            customerId, deliveryNoteId, status, pageIndex, pageSize).ConfigureAwait(false);

        return (total, items.Select(i => i.ToAppDto()).ToList());
    }

    public Task<List<DeliveryNotePickerAppDto>> GetDeliveryNotesByCustomerAsync(Guid customerId)
    {
        var notes = deliveryNoteDataReader.DataSource
            .Where(dn => dn.CustomerId == customerId
                         && dn.SourceType == DeliveryNoteSourceType.ToCustomer
                         && dn.Status == DeliveryNoteStatus.Delivered)
            .OrderByDescending(dn => dn.DeliveredOnUtc)
            .ToList();

        var result = notes.Select(dn => new DeliveryNotePickerAppDto(dn.Id)
        {
            Code = dn.Code,
            DeliveredOnUtc = dn.DeliveredOnUtc ?? dn.CreatedOnUtc
        }).ToList();

        return Task.FromResult(result);
    }

    public Task<List<ReturnableItemAppDto>> GetDeliveryNoteItemsForReturnAsync(
        Guid deliveryNoteId, Guid? excludeReturnId = null)
    {
        var deliveryNote = deliveryNoteDataReader.DataSource
            .FirstOrDefault(dn => dn.Id == deliveryNoteId);
        if (deliveryNote is null)
            return Task.FromResult(new List<ReturnableItemAppDto>());

        // Tính số lượng đã trả theo từng ProductId
        var confirmedReturns = customerReturnDataReader.DataSource
            .Where(r => r.DeliveryNoteId == deliveryNoteId
                        && (int)r.Status == 2 // Confirmed
                        && (excludeReturnId == null || r.Id != excludeReturnId))
            .ToList();

        // Batch load products để lấy unit
        var productIds = deliveryNote.Items.Select(i => i.ProductId).Distinct().ToList();
        var products = productDataReader.DataSource
            .Where(p => productIds.Contains(p.Id))
            .ToList();
        var productDict = products.ToDictionary(p => p.Id);

        // Batch load units
        var unitIds = products
            .Where(p => p.UnitMeasurementId.HasValue)
            .Select(p => p.UnitMeasurementId!.Value)
            .Distinct()
            .ToList();
        var unitDict = unitMeasurementDataReader.DataSource
            .Where(u => unitIds.Contains(u.Id))
            .ToDictionary(u => u.Id, u => u.Name);

        var result = deliveryNote.Items.Select(item =>
        {
            var alreadyReturned = confirmedReturns
                .SelectMany(r => r.Items.Where(i => i.ProductId == item.ProductId))
                .Sum(i => i.AcceptedQuantity);

            productDict.TryGetValue(item.ProductId, out var product);
            var unit = "";
            if (product?.UnitMeasurementId.HasValue == true)
                unitDict.TryGetValue(product.UnitMeasurementId.Value, out unit);

            return new ReturnableItemAppDto
            {
                ProductId = item.ProductId,
                ProductName = item.ProductName,
                Unit = unit ?? "",
                OriginalQty = item.Quantity,
                AlreadyReturnedQty = alreadyReturned,
                UnitPrice = item.UnitPrice,
                SourceItemId = item.Id
            };
        }).ToList();

        return Task.FromResult(result);
    }
}
