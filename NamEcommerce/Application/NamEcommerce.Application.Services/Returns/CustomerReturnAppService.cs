using NamEcommerce.Application.Contracts.Dtos.GoodsReceipts;
using NamEcommerce.Application.Contracts.Dtos.Returns;
using NamEcommerce.Application.Contracts.Returns;
using NamEcommerce.Application.Services.Extensions;
using NamEcommerce.Domain.Entities.Catalog;
using NamEcommerce.Domain.Entities.CustomerPortal;
using NamEcommerce.Domain.Entities.DeliveryNotes;
using NamEcommerce.Domain.Entities.Returns;
using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Domain.Shared.Dtos.Returns;
using NamEcommerce.Domain.Shared.Enums.CustomerPortal;
using NamEcommerce.Domain.Shared.Enums.DeliveryNotes;
using NamEcommerce.Domain.Shared.Enums.Returns;
using NamEcommerce.Domain.Shared.Exceptions.Returns;
using NamEcommerce.Domain.Shared.Helpers;
using NamEcommerce.Domain.Shared.Services.Returns;

namespace NamEcommerce.Application.Services.Returns;

public sealed class CustomerReturnAppService(
    ICustomerReturnManager manager,
    IEntityDataReader<DeliveryNote> deliveryNoteDataReader,
    IEntityDataReader<CustomerReturn> customerReturnDataReader,
    IEntityDataReader<CustomerReturnRequest> customerReturnRequestDataReader,
    IEntityDataReader<Product> productDataReader,
    IEntityDataReader<UnitMeasurement> unitMeasurementDataReader) : ICustomerReturnAppService
{
    private readonly ICustomerReturnManager _manager = manager;

    public async Task<CreateCustomerReturnResultAppDto> CreateAsync(CreateCustomerReturnAppDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var (valid, errorMessage) = dto.Validate();
        if (!valid)
        {
            return new CreateCustomerReturnResultAppDto
            {
                Success = false,
                ErrorMessage = errorMessage
            };
        }

        foreach (var item in dto.Items)
        {
            var product = await productDataReader.GetByIdAsync(item.ProductId).ConfigureAwait(false);
            if (product is null)
            {
                return new CreateCustomerReturnResultAppDto
                {
                    Success = false,
                    ErrorMessage = "Error.GoodsReceipt.ProductIsNotFound"
                };
            }

            if (product.UnitMeasurementId.HasValue)
            {
                var unitMeasurement = await unitMeasurementDataReader.GetByIdAsync(product.UnitMeasurementId.Value).ConfigureAwait(false);
                if (unitMeasurement is not null)
                {
                    if ((new[] { item.RequestedQuantity, item.AcceptedQuantity }).Any(quantity => !NumberHelper.IsValidDecimalPlace(quantity, unitMeasurement.DecimalPlaces)))
                    {
                        return new CreateCustomerReturnResultAppDto
                        {
                            Success = false,
                            ErrorMessage = "Error.QuantityMustBeInteger"
                        };
                    }
                }
            }
        }

        var domainDto = new CreateCustomerReturnDto
        {
            DeliveryNoteId = dto.DeliveryNoteId,
            CustomerId = dto.CustomerId,
            WarehouseId = dto.WarehouseId,
            Note = dto.Note,
            AdditionalCost = dto.AdditionalCost,
            CompensateInNextDelivery = dto.CompensateInNextDelivery,
            ExcludeCustomerReturnRequestId = dto.ExcludeCustomerReturnRequestId,
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

        var result = await _manager.CreateAsync(domainDto).ConfigureAwait(false);
        return new CreateCustomerReturnResultAppDto { Success = true, CreatedId = result.Id };
    }

    public async Task<UpdateCustomerReturnResultAppDto> UpdateAsync(UpdateCustomerReturnAppDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var domainDto = new UpdateCustomerReturnDto(dto.Id)
        {
            Note = dto.Note,
            ReturnDate = dto.ReturnDate
        };

        await _manager.UpdateAsync(domainDto).ConfigureAwait(false);
        return new UpdateCustomerReturnResultAppDto { Success = true };
    }

    public async Task<ConfirmCustomerReturnResultAppDto> MoveToInspectingAsync(Guid id)
    {
        await _manager.MoveToInspectingAsync(id).ConfigureAwait(false);
        return new ConfirmCustomerReturnResultAppDto { Success = true };
    }

    public async Task<ConfirmCustomerReturnResultAppDto> ConfirmAsync(Guid id, Guid? warehouseId = null)
    {
        await _manager.ConfirmAsync(id, warehouseId).ConfigureAwait(false);
        return new ConfirmCustomerReturnResultAppDto { Success = true };
    }

    public async Task<ConfirmCustomerReturnResultAppDto> CancelAsync(Guid id)
    {
        await _manager.CancelAsync(id).ConfigureAwait(false);
        return new ConfirmCustomerReturnResultAppDto { Success = true };
    }

    public async Task<CustomerReturnAppDto?> GetByIdAsync(Guid id)
    {
        var dto = await _manager.GetByIdAsync(id).ConfigureAwait(false);
        return dto?.ToAppDto();
    }

    public async Task<(int Total, List<CustomerReturnAppDto> Items)> GetListAsync(
        int pageIndex, int pageSize, Guid? customerId = null, Guid? deliveryNoteId = null, int? status = null)
    {
        var (total, items) = await _manager.GetListAsync(
            pageIndex, pageSize, customerId, deliveryNoteId, status).ConfigureAwait(false);

        return (total, items.Select(i => i.ToAppDto()).ToList());
    }

    public Task<List<DeliveryNotePickerAppDto>> GetDeliveryNotesByCustomerAsync(Guid customerId)
    {
        var notes = deliveryNoteDataReader.DataSource
            .Where(dn => dn.CustomerId == customerId
                         && (dn.SourceType == DeliveryNoteSourceType.ToCustomer || dn.SourceType == DeliveryNoteSourceType.DirectShipToCustomer)
                         && dn.Status == DeliveryNoteStatus.Delivered)
            .OrderByDescending(dn => dn.DeliveredOnUtc)
            .ToList();

        var result = notes.Select(dn => new DeliveryNotePickerAppDto(dn.Id)
        {
            Code = dn.Code,
            DeliveredOnUtc = dn.DeliveredOnUtc ?? dn.CreatedOnUtc,
            WarehouseId = null
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

        // Tính số lượng đã trả/đang giữ theo từng dòng phiếu giao.
        var activeReturns = customerReturnDataReader.DataSource
            .Where(r => r.Status != CustomerReturnStatus.Cancelled
                        && (excludeReturnId == null || r.Id != excludeReturnId))
            .ToList();
        var activePortalRequests = customerReturnRequestDataReader.DataSource
            .Where(r => r.CustomerId == deliveryNote.CustomerId
                        && (r.Status == CustomerReturnRequestStatus.PendingReview ||
                            r.Status == CustomerReturnRequestStatus.Accepted))
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
        var unitMeasurements = unitMeasurementDataReader.DataSource
            .Where(u => unitIds.Contains(u.Id))
            .ToList();
        var unitDict = unitMeasurements.ToDictionary(u => u.Id, u => u.Name);
        var unitDecimalPlacesDict = unitMeasurements.ToDictionary(u => u.Id, u => u.DecimalPlaces);

        var result = deliveryNote.Items.Select(item =>
        {
            var alreadyReturned = activeReturns
                .SelectMany(r => r.Items.Where(i =>
                    i.DeliveryNoteItemId == item.Id ||
                    (!i.DeliveryNoteItemId.HasValue && r.DeliveryNoteId == deliveryNoteId && i.ProductId == item.ProductId)))
                .Sum(i => i.AcceptedQuantity);

            var pendingPortalReturnQty = activePortalRequests
                .SelectMany(r => r.Items.Where(i => i.DeliveryNoteItemId == item.Id))
                .Sum(i => i.RequestedQuantity);

            productDict.TryGetValue(item.ProductId, out var product);
            var unit = "";
            var decimalPlaces = 0;
            if (product?.UnitMeasurementId.HasValue == true)
            {
                unitDict.TryGetValue(product.UnitMeasurementId.Value, out unit);
                unitDecimalPlacesDict.TryGetValue(product.UnitMeasurementId.Value, out decimalPlaces);
            }

            return new ReturnableItemAppDto
            {
                ProductId = item.ProductId,
                ProductName = item.ProductName,
                Unit = unit ?? "",
                OriginalQty = item.Quantity,
                AlreadyReturnedQty = alreadyReturned + pendingPortalReturnQty,
                UnitPrice = item.UnitPrice,
                SourceItemId = item.Id,
                QuantityDecimalPlaces = decimalPlaces
            };
        }).ToList();

        return Task.FromResult(result);
    }

    public Task<List<ReturnableItemAppDto>> GetReturnableItemsByCustomerAsync(Guid customerId, Guid? excludeReturnId = null)
    {
        if (customerId == Guid.Empty)
            return Task.FromResult(new List<ReturnableItemAppDto>());

        var deliveryNotes = deliveryNoteDataReader.DataSource
            .Where(dn => dn.CustomerId == customerId
                         && (dn.SourceType == DeliveryNoteSourceType.ToCustomer || dn.SourceType == DeliveryNoteSourceType.DirectShipToCustomer)
                         && dn.Status == DeliveryNoteStatus.Delivered)
            .OrderByDescending(dn => dn.DeliveredOnUtc ?? dn.CreatedOnUtc)
            .ToList();

        if (deliveryNotes.Count == 0)
            return Task.FromResult(new List<ReturnableItemAppDto>());

        var deliveryNoteIds = deliveryNotes.Select(dn => dn.Id).ToHashSet();
        var sourceItemIds = deliveryNotes
            .SelectMany(dn => dn.Items.Select(item => item.Id))
            .ToHashSet();

        var activeReturns = customerReturnDataReader.DataSource
            .Where(r => r.CustomerId == customerId
                        && r.Status != CustomerReturnStatus.Cancelled
                        && (excludeReturnId == null || r.Id != excludeReturnId))
            .ToList();
        var activePortalRequests = customerReturnRequestDataReader.DataSource
            .Where(r => r.CustomerId == customerId
                        && (r.Status == CustomerReturnRequestStatus.PendingReview ||
                            r.Status == CustomerReturnRequestStatus.Accepted))
            .ToList();

        var productIds = deliveryNotes
            .SelectMany(dn => dn.Items.Select(item => item.ProductId))
            .Distinct()
            .ToList();
        var products = productDataReader.DataSource
            .Where(p => productIds.Contains(p.Id))
            .ToList();
        var productDict = products.ToDictionary(p => p.Id);
        var unitIds = products
            .Where(p => p.UnitMeasurementId.HasValue)
            .Select(p => p.UnitMeasurementId!.Value)
            .Distinct()
            .ToList();
        var unitDict = unitMeasurementDataReader.DataSource
            .Where(u => unitIds.Contains(u.Id))
            .ToDictionary(u => u.Id, u => u.Name);

        var sourceRows = deliveryNotes
            .SelectMany(dn => dn.Items.Select(item => new
            {
                DeliveryNote = dn,
                Item = item,
                ReservedQuantity = activeReturns
                    .SelectMany(r => r.Items.Where(returnItem =>
                        returnItem.DeliveryNoteItemId == item.Id ||
                        (!returnItem.DeliveryNoteItemId.HasValue
                            && r.DeliveryNoteId == dn.Id
                            && returnItem.ProductId == item.ProductId)))
                    .Sum(returnItem => returnItem.AcceptedQuantity)
                    + activePortalRequests
                        .SelectMany(r => r.Items.Where(requestItem =>
                            sourceItemIds.Contains(requestItem.DeliveryNoteItemId)
                            && requestItem.DeliveryNoteItemId == item.Id))
                        .Sum(requestItem => requestItem.RequestedQuantity)
            }))
            .ToList();

        var result = sourceRows
            .GroupBy(row => row.Item.ProductId)
            .Select(group =>
            {
                productDict.TryGetValue(group.Key, out var product);
                var unit = string.Empty;
                if (product?.UnitMeasurementId.HasValue == true)
                    unitDict.TryGetValue(product.UnitMeasurementId.Value, out unit);

                var latestSource = group
                    .OrderByDescending(row => row.DeliveryNote.DeliveredOnUtc ?? row.DeliveryNote.CreatedOnUtc)
                    .First();

                return new ReturnableItemAppDto
                {
                    ProductId = group.Key,
                    ProductName = product?.Name ?? latestSource.Item.ProductName,
                    Unit = unit ?? string.Empty,
                    OriginalQty = group.Sum(row => row.Item.Quantity),
                    AlreadyReturnedQty = group.Sum(row => row.ReservedQuantity),
                    UnitPrice = latestSource.Item.UnitPrice,
                    SourceItemId = null
                };
            })
            .Where(item => item.OriginalQty - item.AlreadyReturnedQty > 0)
            .OrderBy(item => item.ProductName)
            .ToList();

        return Task.FromResult(result);
    }
}
