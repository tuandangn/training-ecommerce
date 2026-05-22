using MediatR;
using Microsoft.AspNetCore.Mvc;
using NamEcommerce.Web.Contracts.Commands.Models.Debts;
using NamEcommerce.Web.Contracts.Queries.Models.Debts;
using NamEcommerce.Web.Contracts.Models.Debts;
using NamEcommerce.Domain.Shared.Exceptions;

namespace NamEcommerce.Web.Controllers;

public sealed class CustomerDebtController(IMediator mediator) : BaseAuthorizedController
{
    private readonly IMediator _mediator = mediator;

    public async Task<IActionResult> List(string? keywords, int pageIndex = 1)
    {
        var model = await _mediator.Send(new GetCustomerDebtListQuery
        {
            Keywords = keywords,
            PageIndex = pageIndex
        }).ConfigureAwait(false);

        return View(model);
    }

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

    public async Task<IActionResult> Print(Guid customerId)
    {
        var model = await _mediator.Send(new GetCustomerDebtDetailsQuery { CustomerId = customerId }).ConfigureAwait(false);
        if (model == null) return NotFound();
        return View(model);
    }

    [HttpPost]
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

    /// <summary>In biên lai cho 1 lần thanh toán cụ thể.</summary>
    public async Task<IActionResult> Receipt(Guid paymentId)
    {
        var model = await _mediator.Send(new GetCustomerPaymentReceiptQuery { PaymentId = paymentId }).ConfigureAwait(false);
        if (model == null) return NotFound();
        return View(model);
    }
}
