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
        var model = await _mediator.Send(new GetCustomerLedgerListQuery
        {
            Keywords = keywords,
            PageIndex = pageIndex
        });

        return View(model);
    }

    [Authorize(Policy = SystemPermissions.Debts.CustomerDebtsView)]
    public async Task<IActionResult> Details(Guid id, int pageIndex = 1)
    {
        var model = await _mediator.Send(new GetCustomerLedgerDetailsQuery
        {
            CustomerId = id,
            PageIndex = pageIndex
        });

        if (model == null)
        {
            NotifyError("Error.CustomerIsNotFound");
            return RedirectToAction(nameof(List));
        }
        return View(model);
    }

    [Authorize(Policy = SystemPermissions.Debts.CustomerDebtsView)]
    public async Task<IActionResult> Receipt(Guid paymentId)
    {
        var model = await _mediator.Send(new GetCustomerPaymentReceiptQuery { PaymentId = paymentId });
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
            var result = await _mediator.Send(new RecordCustomerPaymentCommand { Model = model });
            return result.Success
                ? Json(new { success = true, message = LocalizeError("Msg.SaveSuccess") })
                : Json(new { success = false, message = LocalizeError(result.ErrorMessage!) });
        }
        catch (NamEcommerceDomainException ex)
        {
            return Json(new { success = false, message = LocalizeError(ex.ErrorCode, ex.Parameters) });
        }
    }
}
