using MediatR;
using NamEcommerce.Application.Contracts.Returns;
using NamEcommerce.Web.Contracts.Models.Returns;
using NamEcommerce.Web.Contracts.Queries.Models.Returns;
using NamEcommerce.Web.Framework.Services;

namespace NamEcommerce.Web.Framework.Queries.Handlers.Returns;

public sealed class GetCustomerReturnHandler : IRequestHandler<GetCustomerReturnQuery, CustomerReturnModel?>
{
    private readonly ICustomerReturnAppService _customerReturnAppService;

    public GetCustomerReturnHandler(ICustomerReturnAppService customerReturnAppService)
    {
        _customerReturnAppService = customerReturnAppService;
    }

    public async Task<CustomerReturnModel?> Handle(GetCustomerReturnQuery request, CancellationToken cancellationToken)
    {
        var dto = await _customerReturnAppService.GetByIdAsync(request.Id).ConfigureAwait(false);
        if (dto is null)
            return null;

        var model = new CustomerReturnModel
        {
            Id = dto.Id,
            Code = dto.Code,
            DeliveryNoteId = dto.DeliveryNoteId,
            DeliveryNoteCode = dto.DeliveryNoteCode,
            CustomerId = dto.CustomerId,
            CustomerName = dto.CustomerName,
            WarehouseId = dto.WarehouseId,
            WarehouseName = dto.WarehouseName,
            Note = dto.Note,
            Status = dto.Status,
            ReturnDate = DateTimeHelper.ToLocalTime(dto.ReturnDate),
            ConfirmedOn = DateTimeHelper.ToLocalTime(dto.ConfirmedOnUtc),
            AdditionalCost = dto.AdditionalCost,
            CompensateInNextDelivery = dto.CompensateInNextDelivery,
            GeneratedGoodsReceiptId = dto.GeneratedGoodsReceiptId,
            CreatedOn = DateTimeHelper.ToLocalTime(dto.CreatedOnUtc),
            UpdatedOn = DateTimeHelper.ToLocalTime(dto.UpdatedOnUtc)
        };

        foreach (var item in dto.Items)
        {
            model.Items.Add(new CustomerReturnModel.ItemModel(item.Id)
            {
                ProductId = item.ProductId,
                ProductName = item.ProductName,
                DeliveryNoteItemId = item.DeliveryNoteItemId,
                RequestedQuantity = item.RequestedQuantity,
                AcceptedQuantity = item.AcceptedQuantity,
                OriginalUnitPrice = item.OriginalUnitPrice,
                ReturnUnitPrice = item.ReturnUnitPrice
            });
        }

        return model;
    }
}
