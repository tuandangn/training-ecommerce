using MediatR;
using NamEcommerce.Domain.Shared.Enums.Returns;
using NamEcommerce.Web.Contracts.Configurations;
using NamEcommerce.Web.Contracts.Models.Returns;
using NamEcommerce.Web.Contracts.Queries.Models.Inventory;
using NamEcommerce.Web.Contracts.Queries.Models.Returns;
using NamEcommerce.Web.Models.Returns;

namespace NamEcommerce.Web.Services.Returns;

public sealed class CustomerReturnModelFactory : ICustomerReturnModelFactory
{
    private readonly AppConfig _appConfig;
    private readonly IMediator _mediator;

    public CustomerReturnModelFactory(AppConfig appConfig, IMediator mediator)
    {
        _appConfig = appConfig;
        _mediator = mediator;
    }

    public async Task<CreateCustomerReturnModel> PrepareCreateCustomerReturnModel(CreateCustomerReturnModel? model = null)
    {
        var warehouses = await _mediator.Send(new GetWarehouseOptionListQuery()).ConfigureAwait(false);

        model ??= new CreateCustomerReturnModel();
        model.AvailableWarehouses = warehouses;

        return model;
    }

    public async Task<CustomerReturnDetailsModel?> PrepareCustomerReturnDetailsModel(Guid id)
    {
        var customerReturn = await _mediator.Send(new GetCustomerReturnQuery { Id = id }).ConfigureAwait(false);
        if (customerReturn is null)
            return null;

        var statusLabel = GetStatusLabel(customerReturn.Status);

        var model = new CustomerReturnDetailsModel
        {
            Id = customerReturn.Id,
            Code = customerReturn.Code,
            OrderId = customerReturn.OrderId,
            OrderCode = customerReturn.OrderCode,
            CustomerId = customerReturn.CustomerId,
            CustomerName = customerReturn.CustomerName,
            WarehouseId = customerReturn.WarehouseId,
            WarehouseName = customerReturn.WarehouseName,
            Note = customerReturn.Note,
            Status = customerReturn.Status,
            StatusLabel = statusLabel,
            ReturnDate = customerReturn.ReturnDate,
            ConfirmedOn = customerReturn.ConfirmedOn,
            GeneratedGoodsReceiptId = customerReturn.GeneratedGoodsReceiptId,
            CreatedOn = customerReturn.CreatedOn,
            UpdatedOn = customerReturn.UpdatedOn
        };

        foreach (var item in customerReturn.Items)
        {
            model.Items.Add(new CustomerReturnDetailsModel.ItemModel(item.Id)
            {
                ProductId = item.ProductId,
                ProductName = item.ProductName,
                DeliveryNoteItemId = item.DeliveryNoteItemId,
                RequestedQuantity = item.RequestedQuantity,
                AcceptedQuantity = item.AcceptedQuantity,
                UnitPrice = item.UnitPrice
            });
        }

        return model;
    }

    public async Task<CustomerReturnListModel> PrepareCustomerReturnListModel(CustomerReturnListSearchModel searchModel)
    {
        var pageNumber = searchModel?.PageNumber ?? 1;
        var pageSize = searchModel?.PageSize ?? 0;
        if (pageNumber <= 0) pageNumber = 1;
        if (pageSize <= 0) pageSize = _appConfig.DefaultPageSize;

        return await _mediator.Send(new GetCustomerReturnListQuery
        {
            CustomerId = searchModel?.CustomerId,
            OrderId = searchModel?.OrderId,
            Status = searchModel?.Status,
            PageIndex = pageNumber - 1,
            PageSize = pageSize
        }).ConfigureAwait(false);
    }

    private static string GetStatusLabel(int status) => (CustomerReturnStatus)status switch
    {
        CustomerReturnStatus.Draft => "Bản nháp",
        CustomerReturnStatus.Inspecting => "Đang kiểm tra",
        CustomerReturnStatus.Confirmed => "Đã xác nhận",
        CustomerReturnStatus.Cancelled => "Đã huỷ",
        _ => "Không xác định"
    };
}
