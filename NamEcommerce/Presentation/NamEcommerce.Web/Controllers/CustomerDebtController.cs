using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NamEcommerce.Web.Contracts.Commands.Models.Debts;
using NamEcommerce.Web.Contracts.Queries.Models.Debts;
using NamEcommerce.Web.Contracts.Models.Debts;
using NamEcommerce.Domain.Shared.Exceptions;
using NamEcommerce.Web.Contracts.Security;

namespace NamEcommerce.Web.Controllers;

public sealed class CustomerDebtController(IMediator mediator) : BaseAuthorizedController
{
    private readonly IMediator _mediator = mediator;

    [Authorize(Policy = SystemPermissions.Debts.CustomerDebtsView)]
    public async Task<IActionResult> List(string? keywords, int pageIndex = 1)
    {
        var model = await _mediator.Send(new GetCustomerDebtListQuery
        {
            Keywords = keywords,
            PageIndex = pageIndex
        }).ConfigureAwait(false);

        return View(model);
    }

    [Authorize(Policy = SystemPermissions.Debts.CustomerDebtsView)]
    public async Task<IActionResult> Details(Guid id)
    {
        var model = await _mediator.Send(new GetCustomerDebtDetailsQuery { CustomerId = id }).ConfigureAwait(false);
        if (model == null)
        {
            NotifyError("Error.CustomerIsNotFound");
            return RedirectToAction(nameof(List));
        }
        return View(model);
    }

    [Authorize(Policy = SystemPermissions.Debts.CustomerDebtsView)]
    public async Task<IActionResult> Print(Guid id)
    {
        var model = await _mediator.Send(new GetCustomerDebtDetailsQuery { CustomerId = id }).ConfigureAwait(false);
        if (model == null) return NotFound();
        return View(model);
    }

    [HttpPost]
    [Authorize(Policy = SystemPermissions.Debts.CustomerDebtsRecordPayment)]
    public async Task<IActionResult> RecordPayment(RecordPaymentModel model)
    {
        if (!ModelState.IsValid)
            return Json(new { success = false, message = LocalizeError("Error.InvalidRequest", GetErrorMessage()) });

        try
        {
            var result = await _mediator.Send(new RecordCustomerPaymentCommand { Model = model }).ConfigureAwait(false);
            return result.Success
                ? Json(new { success = true, message = LocalizeError("Msg.SaveSuccess") })
                : Json(new { success = false, message = LocalizeError(result.ErrorMessage!) });
        }
        catch (NamEcommerceDomainException ex)
        {
            return Json(new
            {
                success = false,
                message = LocalizeError(ex.ErrorCode, ex.Parameters)
            });
        }
    }

    [HttpPost]
    [Authorize(Policy = SystemPermissions.Debts.CustomerDebtsRecordPayment)]
    public async Task<IActionResult> RecordFlexiblePayment(RecordPaymentModel model)
    {
        if (!ModelState.IsValid)
            return Json(new { success = false, message = LocalizeError("Error.InvalidRequest", GetErrorMessage()) });

        try
        {
            var result = await _mediator.Send(new RecordFlexiblePaymentCommand { Model = model }).ConfigureAwait(false);
            return result.Success
                ? Json(new { success = true, message = result.SuccessMessage ?? LocalizeError("Msg.SaveSuccess") })
                : Json(new { success = false, message = LocalizeError(result.ErrorMessage!) });
        }
        catch (NamEcommerceDomainException ex)
        {
            return Json(new { success = false, message = LocalizeError(ex.ErrorCode, ex.Parameters) });
        }
    }

    [Authorize(Policy = SystemPermissions.Debts.CustomerDebtsView)]
    public async Task<IActionResult> Receipt(Guid paymentId)
    {
        var model = await _mediator.Send(new GetCustomerPaymentReceiptQuery { PaymentId = paymentId }).ConfigureAwait(false);
        if (model == null) return NotFound();
        return View(model);
    }
}
