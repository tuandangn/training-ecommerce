using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NamEcommerce.Domain.Entities.GoodsReceipts;
using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Web.Contracts.Commands.Models.Returns;
using NamEcommerce.Web.Contracts.Queries.Models.Inventory;
using NamEcommerce.Web.Contracts.Queries.Models.Returns;
using NamEcommerce.Web.Contracts.Security;
using NamEcommerce.Web.Extensions;
using NamEcommerce.Web.Models.Returns;
using NamEcommerce.Web.Services.Returns;

namespace NamEcommerce.Web.Controllers;

[Authorize(Policy = SystemPermissions.VendorReturns.Manage)]
public sealed class VendorReturnController : BaseAuthorizedController
{
    private readonly IMediator _mediator;
    private readonly IVendorReturnModelFactory _vendorReturnModelFactory;
    private readonly IEntityDataReader<GoodsReceipt> _goodsReceiptDataReader;

    public VendorReturnController(
        IMediator mediator,
        IVendorReturnModelFactory vendorReturnModelFactory,
        IEntityDataReader<GoodsReceipt> goodsReceiptDataReader)
    {
        _mediator = mediator;
        _vendorReturnModelFactory = vendorReturnModelFactory;
        _goodsReceiptDataReader = goodsReceiptDataReader;
    }

    public IActionResult Index() => RedirectToAction(nameof(List));

    public async Task<IActionResult> List(VendorReturnListSearchModel search)
    {
        var model = await _vendorReturnModelFactory.PrepareVendorReturnListModel(search);
        return View(model);
    }

    public async Task<IActionResult> Create(Guid? goodsReceiptId = null,
        Guid? vendorId = null, string? vendorName = null,
        string? goodsReceiptCode = null, Guid? purchaseOrderId = null, string? purchaseOrderCode = null)
    {
        CreateVendorReturnModel? prefilledModel = null;
        if (goodsReceiptId.HasValue || vendorId.HasValue || purchaseOrderId.HasValue)
        {
            prefilledModel = new CreateVendorReturnModel
            {
                GoodsReceiptId = goodsReceiptId,
                GoodsReceiptDisplayLabel = goodsReceiptCode,
                VendorId = vendorId,
                VendorDisplayName = vendorName,
                FilterPurchaseOrderId = purchaseOrderId,
                FilterPurchaseOrderCode = purchaseOrderCode
            };
        }

        var model = await _vendorReturnModelFactory.PrepareCreateVendorReturnModel(prefilledModel);
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateVendorReturnModel model)
    {
        if (!ModelState.IsValid)
        {
            model = await _vendorReturnModelFactory.PrepareCreateVendorReturnModel(model);
            return View(model);
        }

        var returnItems = model.Items
            .Where(i => i.RequestedQuantity > 0)
            .ToList();

        var missingProductRows = returnItems
            .Where(item => !item.ProductId.HasValue && item.GoodsReceiptItemId.HasValue)
            .ToList();
        if (missingProductRows.Count > 0)
        {
            var missingSourceItemIds = missingProductRows
                .Select(item => item.GoodsReceiptItemId!.Value)
                .Distinct()
                .ToHashSet();
            var sourceProductByGoodsReceiptItemId = _goodsReceiptDataReader.DataSource
                .SelectMany(receipt => receipt.Items)
                .Where(item => missingSourceItemIds.Contains(item.Id))
                .ToDictionary(item => item.Id, item => item.ProductId);

            foreach (var item in missingProductRows)
            {
                if (item.GoodsReceiptItemId.HasValue &&
                    sourceProductByGoodsReceiptItemId.TryGetValue(item.GoodsReceiptItemId.Value, out var productId))
                {
                    item.ProductId = productId;
                }
            }
        }

        returnItems = returnItems
            .Where(i => i.ProductId.HasValue)
            .ToList();

        if (!returnItems.Any())
        {
            AddLocalizedModelError("Error.VendorReturn.NoItems");
            model = await _vendorReturnModelFactory.PrepareCreateVendorReturnModel(model);
            return View(model);
        }

        if (!model.VendorId.HasValue)
        {
            AddLocalizedModelError("Error.VendorReturn.VendorRequired");
            model = await _vendorReturnModelFactory.PrepareCreateVendorReturnModel(model);
            return View(model);
        }

        var result = await _mediator.Send(new CreateVendorReturnCommand
        {
            VendorId = model.VendorId.Value,
            GoodsReceiptId = model.GoodsReceiptId,
            WarehouseId = model.WarehouseId,
            AdditionalCost = model.AdditionalCost ?? 0,
            Note = model.Note,
            Items = returnItems.Select(i => new CreateVendorReturnItemCommand
            {
                ProductId = i.ProductId!.Value,
                GoodsReceiptItemId = i.GoodsReceiptItemId,
                RequestedQuantity = i.RequestedQuantity,
                // AcceptedQuantity không có field riêng trong form — default = RequestedQuantity (Draft)
                AcceptedQuantity = i.AcceptedQuantity > 0 ? i.AcceptedQuantity : i.RequestedQuantity,
                OriginalUnitCost = i.OriginalUnitCost,
                ReturnUnitCost = i.ReturnUnitCost
            }).ToList()
        });

        if (!result.Success)
        {
            AddLocalizedModelError(result.ErrorMessage);
            model = await _vendorReturnModelFactory.PrepareCreateVendorReturnModel(model);
            return View(model);
        }

        NotifySuccess("Msg.SaveSuccess");
        return RedirectToAction(nameof(Details), new { id = result.CreatedId });
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var model = await _vendorReturnModelFactory.PrepareVendorReturnDetailsModel(id);
        if (model is null)
        {
            NotifyError("Error.VendorReturn.IsNotFound");
            return RedirectToAction(nameof(List));
        }

        ViewBag.AvailableWarehouses = await _mediator.Send(new GetWarehouseOptionListQuery());

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Update(Guid id, string? note, DateTime? returnDate)
    {
        var result = await _mediator.Send(new UpdateVendorReturnCommand
        {
            Id = id,
            Note = note,
            ReturnDate = returnDate
        });

        if (!result.Success)
            return this.JsonError(LocalizeError(result.ErrorMessage!));

        return this.JsonOk();
    }

    [HttpPost]
    public async Task<IActionResult> MoveToInspecting(Guid id)
    {
        var result = await _mediator.Send(new MoveVendorReturnToInspectingCommand { Id = id });

        if (!result.Success)
            return this.JsonError(LocalizeError(result.ErrorMessage!));

        return this.JsonOk();
    }

    [HttpPost]
    public async Task<IActionResult> Confirm(Guid id, Guid? warehouseId = null)
    {
        if (!warehouseId.HasValue || warehouseId.Value == Guid.Empty)
            return Json(new { success = false, message = LocalizeError("Error.VendorReturn.WarehouseRequired") });

        var result = await _mediator.Send(new ConfirmVendorReturnCommand { Id = id, WarehouseId = warehouseId });

        if (!result.Success)
            return this.JsonError(LocalizeError(result.ErrorMessage!));

        return this.JsonOk();
    }

    [HttpPost]
    public async Task<IActionResult> Cancel(Guid id)
    {
        var (success, errorMessage) = await _mediator.Send(new CancelVendorReturnCommand { Id = id });

        if (!success)
        {
            NotifyError(errorMessage!);
        }
        else
        {
            NotifySuccess("Msg.CancelSuccess");
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    public async Task<IActionResult> Reverse(Guid id, string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            NotifyError("Error.VendorReturn.ReverseReasonRequired");
            return RedirectToAction(nameof(Details), new { id });
        }

        var (success, errorMessage) = await _mediator.Send(new ReverseVendorReturnCommand { Id = id, Reason = reason });

        if (!success)
            NotifyError(errorMessage!);
        else
            NotifySuccess("Msg.SaveSuccess");

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpGet]
    public async Task<IActionResult> GetById(Guid id)
    {
        var model = await _mediator.Send(new GetVendorReturnQuery { Id = id });
        if (model is null)
            return Json(new { success = false });

        return Json(new { success = true, data = model });
    }

    [HttpGet]
    public async Task<IActionResult> GetGoodsReceipts(Guid vendorId, Guid? purchaseOrderId = null)
    {
        var receipts = await _mediator.Send(new GetGoodsReceiptsByVendorQuery
        {
            VendorId = vendorId,
            PurchaseOrderId = purchaseOrderId
        });
        return Json(receipts);
    }

    [HttpPost]
    public async Task<IActionResult> GetValidWarehouses([FromBody] GetValidWarehousesForReturnQuery query)
    {
        var ids = await _mediator.Send(query ?? new GetValidWarehousesForReturnQuery());
        return Json(ids);
    }

    [HttpGet]
    public async Task<IActionResult> GetGoodsReceiptItems(Guid goodsReceiptId, Guid? excludeReturnId = null)
    {
        var items = await _mediator.Send(new GetGoodsReceiptItemsForReturnQuery
        {
            GoodsReceiptId = goodsReceiptId,
            ExcludeReturnId = excludeReturnId
        });
        return Json(items);
    }

    [HttpGet]
    public async Task<IActionResult> GetByGoodsReceipt(Guid goodsReceiptId)
    {
        var result = await _mediator.Send(new GetVendorReturnListQuery
        {
            GoodsReceiptId = goodsReceiptId,
            PageIndex = 0,
            PageSize = 100
        });
        return Json(result.Data.Items.Select(r => new
        {
            id = r.Id,
            code = r.Code,
            status = r.Status,
            statusLabel = r.Status switch
            {
                0 => "Bản nháp",
                1 => "Đang kiểm hàng",
                2 => "Đã xác nhận",
                3 => "Đã hủy",
                _ => "?"
            },
            statusColor = r.Status switch
            {
                0 => "secondary",
                1 => "warning",
                2 => "success",
                3 => "danger",
                _ => "secondary"
            },
            returnDate = r.ReturnDate.ToLocalTime().ToString("dd/MM/yyyy"),
            totalAmount = r.TotalAmount,
            itemCount = r.ItemCount
        }));
    }
}
