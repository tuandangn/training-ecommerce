using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NamEcommerce.Web.Contracts.Commands.Models.Debts;
using NamEcommerce.Web.Contracts.Models.Debts;
using NamEcommerce.Web.Contracts.Queries.Models.Debts;
using NamEcommerce.Web.Contracts.Security;

namespace NamEcommerce.Web.Controllers;

public sealed class VendorDebtController(IMediator mediator) : BaseAuthorizedController
{
    private readonly IMediator _mediator = mediator;

    [Authorize(Policy = SystemPermissions.Debts.VendorDebtsView)]
    public async Task<IActionResult> Index(string? keywords, int pageIndex = 1)
    {
        var model = await _mediator.Send(new GetVendorLedgerListQuery
        {
            Keywords = keywords,
            PageIndex = pageIndex
        }).ConfigureAwait(false);

        return View(model);
    }

    [Authorize(Policy = SystemPermissions.Debts.VendorDebtsView)]
    public async Task<IActionResult> Details(Guid vendorId, int pageIndex = 1)
    {
        var model = await _mediator.Send(new GetVendorLedgerDetailsQuery
        {
            VendorId = vendorId,
            PageIndex = pageIndex
        }).ConfigureAwait(false);
        if (model == null)
        {
            NotifyError("Error.VendorDebtNotFound");
            return RedirectToAction(nameof(Index));
        }
        return View(model);
    }

    [HttpPost]
    [Authorize(Policy = SystemPermissions.Debts.VendorDebtsRecordPayment)]
    public async Task<IActionResult> RecordPayment(RecordVendorPaymentModel model)
    {
        if (!ModelState.IsValid)
            return Json(new { success = false, message = LocalizeError("Error.InvalidRequest", GetErrorMessage()) });

        var result = await _mediator.Send(new RecordVendorPaymentCommand { Model = model }).ConfigureAwait(false);
        return result.Success
            ? Json(new { success = true, message = LocalizeError("Msg.SaveSuccess") })
            : Json(new { success = false, message = LocalizeError(result.ErrorMessage!) });
    }

    [HttpPost]
    [Authorize(Policy = SystemPermissions.Debts.VendorDebtsRecordPayment)]
    public async Task<IActionResult> RecordFlexiblePayment(RecordVendorPaymentModel model)
    {
        if (!ModelState.IsValid)
            return Json(new { success = false, message = LocalizeError("Error.InvalidRequest", GetErrorMessage()) });

        var result = await _mediator.Send(new RecordFlexibleVendorPaymentCommand { Model = model }).ConfigureAwait(false);
        return result.Success
            ? Json(new { success = true, message = result.SuccessMessage ?? LocalizeError("Msg.SaveSuccess") })
            : Json(new { success = false, message = LocalizeError(result.ErrorMessage!) });
    }

    [HttpPost]
    [Authorize(Policy = SystemPermissions.Debts.VendorDebtsRecordPayment)]
    public async Task<IActionResult> RecordAdvance(RecordVendorPaymentModel model)
    {
        if (!ModelState.IsValid)
            return Json(new { success = false, message = LocalizeError("Error.InvalidRequest", GetErrorMessage()) });

        var result = await _mediator.Send(new RecordVendorAdvancePaymentCommand { Model = model }).ConfigureAwait(false);
        return result.Success
            ? Json(new { success = true, message = LocalizeError("Msg.SaveSuccess") })
            : Json(new { success = false, message = LocalizeError(result.ErrorMessage!) });
    }

    [Authorize(Policy = SystemPermissions.Debts.VendorDebtsView)]
    public async Task<IActionResult> Receipt(Guid paymentId)
    {
        var model = await _mediator.Send(new GetVendorPaymentReceiptQuery { PaymentId = paymentId }).ConfigureAwait(false);
        if (model == null) return NotFound();
        return View(model);
    }
}
