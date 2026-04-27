using MediatR;
using Microsoft.AspNetCore.Mvc;
using NamEcommerce.Domain.Shared.Settings;
using NamEcommerce.Web.Contracts.Commands.Models.GoodsReceipts;
using NamEcommerce.Web.Contracts.Queries.Models.GoodsReceipts;
using NamEcommerce.Web.Contracts.Queries.Models.PurchaseOrders;
using NamEcommerce.Web.Models.GoodsReceipts;
using NamEcommerce.Web.Services.GoodsReceipts;

namespace NamEcommerce.Web.Controllers;

public sealed class GoodsReceiptController : BaseAuthorizedController
{
    private readonly IMediator _mediator;
    private readonly IGoodsReceiptModelFactory _goodsReceiptModelFactory;
    private readonly WarehouseSettings _warehouseSettings;

    public GoodsReceiptController(IMediator mediator, IGoodsReceiptModelFactory goodsReceiptModelFactory, WarehouseSettings warehouseSettings)
    {
        _mediator = mediator;
        _goodsReceiptModelFactory = goodsReceiptModelFactory;
        _warehouseSettings = warehouseSettings;
    }

    public IActionResult Index() => RedirectToAction(nameof(List));

    // ─────────────────────────── LIST ───────────────────────────

    public async Task<IActionResult> List(GoodsReceiptListSearchModel search)
    {
        var model = await _goodsReceiptModelFactory.PrepareGoodsReceiptListModel(search);
        return View(model);
    }

    // ─────────────────────────── CREATE ───────────────────────────

    public async Task<IActionResult> Create()
    {
        var model = await _goodsReceiptModelFactory.PrepareCreateGoodsReceiptModel();
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateGoodsReceiptModel model)
    {
        if (!ModelState.IsValid)
        {
            model = await _goodsReceiptModelFactory.PrepareCreateGoodsReceiptModel(model);
            return View(model);
        }

        var result = await _mediator.Send(new CreateGoodsReceiptCommand
        {
            ReceivedOn = model.ReceivedOn,
            TruckDriverName = model.TruckDriverName,
            TruckNumberSerial = model.TruckNumberSerial,
            Note = model.Note,
            PictureIds = model.PictureIds,
            VendorId = model.VendorId,
            Items = model.Items.Select(i => new CreateGoodsReceiptItemCommand
            {
                ProductId = i.ProductId!.Value,
                WarehouseId = i.WarehouseId,
                Quantity = i.Quantity,
                UnitCost = i.UnitCost
            }).ToList()
        });

        if (!result.Success)
        {
            AddLocalizedModelError(result.ErrorMessage);
            model = await _goodsReceiptModelFactory.PrepareCreateGoodsReceiptModel(model);
            return View(model);
        }

        NotifySuccess("Msg.SaveSuccess");
        return RedirectToAction(nameof(Details), new { id = result.CreatedId });
    }

    // ─────────────────────────── DETAILS ───────────────────────────

    public async Task<IActionResult> Details(Guid id)
    {
        var model = await _goodsReceiptModelFactory.PrepareGoodsReceiptDetailsModel(id);
        if (model is null)
        {
            NotifyError("Error.GoodsReceipt.IsNotFound");
            return RedirectToAction(nameof(List));
        }

        return View(model);
    }

    // ─────────────────────────── EDIT INFO + PICTURES (JSON) ───────────────────────────

    [HttpPost]
    public async Task<IActionResult> EditInfo(EditGoodsReceiptModel model)
    {
        if (!ModelState.IsValid)
            return Json(new { success = false, message = GetErrorMessage() });

        var goodsReceipt = await _mediator.Send(new GetGoodsReceiptQuery { Id = model.Id });
        if (goodsReceipt is null)
            return Json(new { success = false, message = LocalizeError("Error.GoodsReceipt.IsNotFound") });

        var result = await _mediator.Send(new UpdateGoodsReceiptCommand
        {
            Id = model.Id,
            CreatedOn = model.CreatedOn,
            TruckDriverName = model.TruckDriverName,
            TruckNumberSerial = model.TruckNumberSerial,
            Note = model.Note,
            PictureIds = goodsReceipt.PictureIds.ToList()
        });

        if (!result.Success)
            return Json(new { success = false, message = LocalizeError(result.ErrorMessage!) });

        return Json(new { success = true, message = string.Empty });
    }

    [HttpPost]
    public async Task<IActionResult> UpdatePictures(Guid id, IList<Guid> pictureIds)
    {
        var goodsReceipt = await _mediator.Send(new GetGoodsReceiptQuery { Id = id });
        if (goodsReceipt is null)
            return Json(new { success = false, message = LocalizeError("Error.GoodsReceipt.IsNotFound") });

        var result = await _mediator.Send(new UpdateGoodsReceiptCommand
        {
            Id = id,
            CreatedOn = goodsReceipt.ReceivedOn,
            TruckDriverName = goodsReceipt.TruckDriverName,
            TruckNumberSerial = goodsReceipt.TruckNumberSerial,
            Note = goodsReceipt.Note,
            PictureIds = pictureIds
        });

        if (!result.Success)
            return Json(new { success = false, message = LocalizeError(result.ErrorMessage!) });

        return Json(new { success = true });
    }

    // ─────────────────────────── SET ITEM UNIT COST (JSON) ───────────────────────────

    /// <summary>
    /// Thiết lập đơn giá nhập cho một item còn pending costing.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> SetItemUnitCost(SetGoodsReceiptItemUnitCostModel model)
    {
        if (!ModelState.IsValid)
            return Json(new { success = false, message = GetErrorMessage() });

        var result = await _mediator.Send(new SetGoodsReceiptItemUnitCostCommand
        {
            GoodsReceiptId = model.GoodsReceiptId,
            GoodsReceiptItemId = model.ItemId,
            UnitCost = model.UnitCost
        });

        if (!result.Success)
            return Json(new { success = false, message = LocalizeError(result.ErrorMessage!) });

        return Json(new { success = true, message = string.Empty });
    }

    // ─────────────────────────── SET VENDOR (JSON) ───────────────────────────

    /// <summary>
    /// Gắn hoặc xoá nhà cung cấp khỏi phiếu nhập kho.
    /// POST body: { goodsReceiptId: Guid, vendorId: Guid? }
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> SetVendor(Guid goodsReceiptId, Guid? vendorId)
    {
        var result = await _mediator.Send(new SetGoodsReceiptVendorCommand
        {
            GoodsReceiptId = goodsReceiptId,
            VendorId = vendorId
        });

        if (!result.Success)
            return Json(new { success = false, message = LocalizeError(result.ErrorMessage!) });

        return Json(new { success = true, updatedId = result.UpdatedId });
    }

    // ─────────────────────────── DELETE ───────────────────────────

    [HttpPost]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _mediator.Send(new DeleteGoodsReceiptCommand { Id = id });

        if (!result.Success)
            NotifyError(result.ErrorMessage!);
        else
            NotifySuccess("Msg.DeleteSuccess");

        return RedirectToAction(nameof(List));
    }

    [HttpPost]
    public async Task<IActionResult> SetToPurchaseOrder(Guid id, string code)
    {
        if (code == null)
            return Json(new { success = false, message = LocalizeError("Error.Required", LocalizeError("Label.Code")) });

        var goodsReceipt = await _mediator.Send(new GetGoodsReceiptQuery { Id = id });
        if (goodsReceipt is null)
            return Json(new { success = false, message = LocalizeError("Error.GoodsReceipt.IsNotFound") });

        var purchaseOrderId = await _mediator.Send(new GetPurchaseOrderByCodeQuery(code));
        if (!purchaseOrderId.HasValue)
            return Json(new { success = false, message = LocalizeError("Error.PurchaseOrderIsNotFound") });

        var result = await _mediator.Send(new SetGoodsReceiptToPurchaseOrderCommand(id, purchaseOrderId.Value));
        if (result.Success)
            return Json(new { success = true, message = string.Empty });

        return Json(new { success = false, message = !string.IsNullOrEmpty(result.ErrorMessage) ? LocalizeError(result.ErrorMessage) : "Process is error" });
    }
}
