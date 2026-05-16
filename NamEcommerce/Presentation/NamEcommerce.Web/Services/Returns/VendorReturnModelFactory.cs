using MediatR;
using NamEcommerce.Web.Contracts.Configurations;
using NamEcommerce.Web.Contracts.Models.Returns;
using NamEcommerce.Web.Contracts.Queries.Models.Inventory;
using NamEcommerce.Web.Contracts.Queries.Models.Returns;
using NamEcommerce.Web.Models.Returns;

namespace NamEcommerce.Web.Services.Returns;

public sealed class VendorReturnModelFactory : IVendorReturnModelFactory
{
    private readonly AppConfig _appConfig;
    private readonly IMediator _mediator;

    public VendorReturnModelFactory(AppConfig appConfig, IMediator mediator)
    {
        _appConfig = appConfig;
        _mediator = mediator;
    }

    public async Task<CreateVendorReturnModel> PrepareCreateVendorReturnModel(CreateVendorReturnModel? model = null)
    {
        var warehouses = await _mediator.Send(new GetWarehouseOptionListQuery()).ConfigureAwait(false);

        model ??= new CreateVendorReturnModel();
        model.AvailableWarehouses = warehouses;

        return model;
    }

    public async Task<VendorReturnModel?> PrepareVendorReturnDetailsModel(Guid id)
    {
        return await _mediator.Send(new GetVendorReturnQuery { Id = id }).ConfigureAwait(false);
    }

    public async Task<VendorReturnListModel> PrepareVendorReturnListModel(VendorReturnListSearchModel searchModel)
    {
        var pageNumber = searchModel?.PageNumber ?? 1;
        var pageSize = searchModel?.PageSize ?? 0;
        if (pageNumber <= 0) pageNumber = 1;
        if (pageSize <= 0) pageSize = _appConfig.DefaultPageSize;

        return await _mediator.Send(new GetVendorReturnListQuery
        {
            VendorId = searchModel?.VendorId,
            PurchaseOrderId = searchModel?.PurchaseOrderId,
            GoodsReceiptId = searchModel?.GoodsReceiptId,
            Status = searchModel?.Status,
            PageIndex = pageNumber - 1,
            PageSize = pageSize
        }).ConfigureAwait(false);
    }
}
