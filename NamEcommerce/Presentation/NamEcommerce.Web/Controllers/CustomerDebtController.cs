using MediatR;
using Microsoft.AspNetCore.Mvc;
using NamEcommerce.Web.Constants;
using NamEcommerce.Web.Contracts.Commands.Models.Debts;
using NamEcommerce.Web.Contracts.Queries.Models.Debts;
using NamEcommerce.Web.Contracts.Models.Debts;

namespace NamEcommerce.Web.Controllers;

public sealed class CustomerDebtController(IMediator mediator) : BaseAuthorizedController
{
    private readonly IMediator _mediator = mediator;

    public async Task<IActionResult> List(Guid? customerId, string? keywords, int pageIndex = 1)
    {
        var model = await _mediator.Send(new GetCustomerDebtListQuery 
        { 
            CustomerId = customerId,
            Keywords = keywords,
            PageIndex = pageIndex 
        }).ConfigureAwait(false);
        
        return View(model);
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var model = await _mediator.Send(new GetCustomerDebtDetailsQuery { Id = id }).ConfigureAwait(false);
        if (model == null)
        {
            TempData[ViewConstants.CustomerErrorMessage] = "Không tìm thấy thông tin công nợ.";
            return RedirectToAction(nameof(List));
        }
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> RecordPayment(RecordPaymentModel model)
    {
        if (!ModelState.IsValid)
        {
            return Json(new { success = false, message = "Thông tin không hợp lệ." });
        }

        var result = await _mediator.Send(new RecordCustomerPaymentCommand { Model = model }).ConfigureAwait(false);
        if (result.Success)
        {
            return Json(new { success = true, message = "Đã ghi nhận thanh toán thành công." });
        }

        return Json(new { success = false, message = result.ErrorMessage });
    }

    public async Task<IActionResult> Print(Guid id)
    {
        var model = await _mediator.Send(new GetCustomerDebtDetailsQuery { Id = id }).ConfigureAwait(false);
        if (model == null) return NotFound();

        return View(model);
    }

    public async Task<IActionResult> Receipt(Guid paymentId)
    {
        // This would print a specific receipt for a single payment
        // For now, let's just focus on Print Debt details (as a statement)
        return RedirectToAction(nameof(Print), new { id = paymentId }); 
    }
}
