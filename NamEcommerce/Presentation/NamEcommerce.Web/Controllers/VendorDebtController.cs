using MediatR;
using Microsoft.AspNetCore.Mvc;
using NamEcommerce.Web.Contracts.Commands.Models.Debts;
using NamEcommerce.Web.Contracts.Models.Debts;
using NamEcommerce.Web.Contracts.Queries.Models.Debts;

namespace NamEcommerce.Web.Controllers;

public sealed class VendorDebtController(IMediator mediator) : BaseAuthorizedController
{
    private readonly IMediator _mediator = mediator;

    /// <summary>Trang danh sách: hiển thị NCC có công nợ (gom nhóm).</summary>
    public async Task<IActionResult> Index(string? keywords, int pageIndex = 1)
    {
        var model = await _mediator.Send(new GetVendorDebtListQuery
        {
            Keywords = keywords,
            PageIndex = pageIndex
        }).ConfigureAwait(false);

        return View(model);
    }

    /// <summary>Trang chi tiết: toàn bộ công nợ + tiền ứng trước của 1 NCC.</summary>
    public async Task<IActionResult> Details(Guid vendorId)
    {
        var model = await _mediator.Send(new GetVendorDebtDetailsQuery { VendorId = vendorId }).ConfigureAwait(false);
        if (model == null)
        {
            NotifyError("Error.VendorDebtNotFound");
            return RedirectToAction(nameof(Index));
        }
        return View(model);
    }

    /// <summary>Ghi nhận thanh toán cho 1 phiếu nợ cụ thể (POST).</summary>
    [HttpPost]
    public async Task<IActionResult> RecordPayment(RecordVendorPaymentModel model)
    {
        if (!ModelState.IsValid)
            return Json(new { success = false, message = LocalizeError("Error.InvalidRequest") });

        var result = await _mediator.Send(new RecordVendorPaymentCommand { Model = model }).ConfigureAwait(false);
        return result.Success
            ? Json(new { success = true, message = LocalizeError("Msg.SaveSuccess") })
            : Json(new { success = false, message = LocalizeError(result.ErrorMessage!) });
    }

    /// <summary>Thanh toán linh động FIFO — phân bổ vào các phiếu nợ còn lại (POST).</summary>
    [HttpPost]
    public async Task<IActionResult> RecordFlexiblePayment(RecordVendorPaymentModel model)
    {
        if (!ModelState.IsValid)
            return Json(new { success = false, message = LocalizeError("Error.InvalidRequest") });

        var result = await _mediator.Send(new RecordFlexibleVendorPaymentCommand { Model = model }).ConfigureAwait(false);
        return result.Success
            ? Json(new { success = true, message = result.SuccessMessage ?? LocalizeError("Msg.SaveSuccess") })
            : Json(new { success = false, message = LocalizeError(result.ErrorMessage!) });
    }

    /// <summary>Ghi nhận tiền ứng trước cho NCC (POST).</summary>
    [HttpPost]
    public async Task<IActionResult> RecordAdvance(RecordVendorPaymentModel model)
    {
        if (!ModelState.IsValid)
            return Json(new { success = false, message = LocalizeError("Error.InvalidRequest") });

        var result = await _mediator.Send(new RecordVendorAdvancePaymentCommand { Model = model }).ConfigureAwait(false);
        return result.Success
            ? Json(new { success = true, message = LocalizeError("Msg.SaveSuccess") })
            : Json(new { success = false, message = LocalizeError(result.ErrorMessage!) });
    }

    /// <summary>In phiếu chi cho 1 lần thanh toán NCC.</summary>
    public async Task<IActionResult> Receipt(Guid paymentId)
    {
        var model = await _mediator.Send(new GetVendorPaymentReceiptQuery { PaymentId = paymentId }).ConfigureAwait(false);
        if (model == null) return NotFound();
        return View(model);
    }
}
