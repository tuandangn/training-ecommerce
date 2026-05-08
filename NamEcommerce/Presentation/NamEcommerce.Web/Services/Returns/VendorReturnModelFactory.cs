using MediatR;
using NamEcommerce.Domain.Shared.Enums.Returns;
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

    public async Task<VendorReturnDetailsModel?> PrepareVendorReturnDetailsModel(Guid id)
    {
        var vendorReturn = await _mediator.Send(new GetVendorReturnQuery { Id = id }).ConfigureAwait(false);
        if (vendorReturn is null)
            return null;

        var statusLabel = GetStatusLabel(vendorReturn.Status);

        var model = new VendorReturnDetailsModel
        {
            Id = vendorReturn.Id,
            Code = vendorReturn.Code,
            VendorId = vendorReturn.VendorId,
            VendorName = vendorReturn.VendorName,
            PurchaseOrderId = vendorReturn.PurchaseOrderId,
            GoodsReceiptId = vendorReturn.GoodsReceiptId,
            WarehouseId = vendorReturn.WarehouseId,
            WarehouseName = vendorReturn.WarehouseName,
            Note = vendorReturn.Note,
            Status = vendorReturn.Status,
            StatusLabel = statusLabel,
            ReturnDate = vendorReturn.ReturnDate,
            ConfirmedOn = vendorReturn.ConfirmedOn,
            GeneratedDeliveryNoteId = vendorReturn.GeneratedDeliveryNoteId,
            CreatedOn = vendorReturn.CreatedOn,
            UpdatedOn = vendorReturn.UpdatedOn
        };

        foreach (var item in vendorReturn.Items)
        {
            model.Items.Add(new VendorReturnDetailsModel.ItemModel(item.Id)
            {
                ProductId = item.ProductId,
                ProductName = item.ProductName,
                GoodsReceiptItemId = item.GoodsReceiptItemId,
                RequestedQuantity = item.RequestedQuantity,
                AcceptedQuantity = item.AcceptedQuantity,
                UnitCost = item.UnitCost
            });
        }

        return model;
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

    private static string GetStatusLabel(int status) => (VendorReturnStatus)status switch
    {
        VendorReturnStatus.Draft => "Bản nháp",
        VendorReturnStatus.Inspecting => "Đang kiểm tra",
        VendorReturnStatus.Confirmed => "Đã xác nhận",
        VendorReturnStatus.Cancelled => "Đã huỷ",
        _ => "Không xác định"
    };
}
