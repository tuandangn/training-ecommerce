using MediatR;
using Microsoft.AspNetCore.Mvc;
using NamEcommerce.Web.Contracts.Commands.Models.Returns;
using NamEcommerce.Web.Contracts.Queries.Models.Returns;
using NamEcommerce.Web.Models.Returns;
using NamEcommerce.Web.Services.Returns;

namespace NamEcommerce.Web.Controllers;

public sealed class VendorReturnController : BaseAuthorizedController
{
    private readonly IMediator _mediator;
    private readonly IVendorReturnModelFactory _vendorReturnModelFactory;

    public VendorReturnController(IMediator mediator, IVendorReturnModelFactory vendorReturnModelFactory)
    {
        _mediator = mediator;
        _vendorReturnModelFactory = vendorReturnModelFactory;
    }

    public IActionResult Index() => RedirectToAction(nameof(List));

    public async Task<IActionResult> List(VendorReturnListSearchModel search)
    {
        var model = await _vendorReturnModelFactory.PrepareVendorReturnListModel(search);
        return View(model);
    }

    public async Task<IActionResult> Create(
        Guid? goodsReceiptId = null,
        Guid? vendorId = null,
        string? vendorName = null,
        string? goodsReceiptCode = null)
    {
        CreateVendorReturnModel? prefilledModel = null;
        if (goodsReceiptId.HasValue || vendorId.HasValue)
        {
            prefilledModel = new CreateVendorReturnModel
            {
                GoodsReceiptId = goodsReceiptId,
                GoodsReceiptDisplayLabel = goodsReceiptCode,
                VendorId = vendorId,
                VendorDisplayName = vendorName
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

        var result = await _mediator.Send(new CreateVendorReturnCommand
        {
            VendorId = model.VendorId!.Value,
            GoodsReceiptId = model.GoodsReceiptId,
            WarehouseId = model.WarehouseId!.Value,
            AdditionalCost = model.AdditionalCost,
            Note = model.Note,
            Items = model.Items.Select(i => new CreateVendorReturnItemCommand
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
            return Json(new { success = false, message = LocalizeError(result.ErrorMessage!) });

        return Json(new { success = true });
    }

    [HttpPost]
    public async Task<IActionResult> MoveToInspecting(Guid id)
    {
        var result = await _mediator.Send(new MoveVendorReturnToInspectingCommand { Id = id });

        if (!result.Success)
            return Json(new { success = false, message = LocalizeError(result.ErrorMessage!) });

        return Json(new { success = true });
    }

    [HttpPost]
    public async Task<IActionResult> Confirm(Guid id)
    {
        var result = await _mediator.Send(new ConfirmVendorReturnCommand { Id = id });

        if (!result.Success)
            return Json(new { success = false, message = LocalizeError(result.ErrorMessage!) });

        return Json(new { success = true });
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

    // ── AJAX Pickers ──────────────────────────────────────────────────────────────

    /// <summary>
    /// GET /VendorReturn/GetGoodsReceipts?vendorId=
    /// Trả về danh sách phiếu nhập kho của NCC — dùng cho AJAX dropdown trong form tạo phiếu trả NCC.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetGoodsReceipts(Guid vendorId)
    {
        var receipts = await _mediator.Send(new GetGoodsReceiptsByVendorQuery { VendorId = vendorId });
        return Json(receipts);
    }

    /// <summary>
    /// GET /VendorReturn/GetGoodsReceiptItems?goodsReceiptId=&amp;excludeReturnId=
    /// Trả về danh sách mặt hàng có thể trả của phiếu nhập — bao gồm số lượng đã trả.
    /// </summary>
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

    /// <summary>
    /// GET /VendorReturn/GetByGoodsReceipt?goodsReceiptId=
    /// Trả về danh sách phiếu trả NCC liên kết với phiếu nhập kho — dùng cho section "Trả hàng NCC" trên Details.
    /// </summary>
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
