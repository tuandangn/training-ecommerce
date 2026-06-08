using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NamEcommerce.Web.Contracts.Commands.Models.Debts;
using NamEcommerce.Web.Contracts.Security;
using NamEcommerce.Web.Models.Debts;
using NamEcommerce.Web.Services.Debts;

namespace NamEcommerce.Web.Controllers;

[Authorize(Policy = SystemPermissions.Debts.CustomerRefundsManage)]
public sealed class CustomerRefundController : BaseAuthorizedController
{
    private readonly IMediator _mediator;
    private readonly ICustomerRefundModelFactory _modelFactory;

    public CustomerRefundController(IMediator mediator, ICustomerRefundModelFactory modelFactory)
    {
        _mediator = mediator;
        _modelFactory = modelFactory;
    }

    public IActionResult Index() => RedirectToAction(nameof(List));

    public async Task<IActionResult> List(CustomerRefundListSearchModel search)
    {
        var model = await _modelFactory.PrepareRefundListSearchModel(search);
        return View(model);
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var model = await _modelFactory.PrepareRefundDetailsModel(id);
        if (model is null)
            return NotFound();
        return View(model);
    }

    /// <summary>
    /// Xác nhận đã hoàn tiền cho khách (Pending → Completed). POST → JSON response.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Complete(Guid id, [FromBody] CompleteRefundModel form)
    {
        var result = await _mediator.Send(new CompleteCustomerRefundCommand
        {
            Id = id,
            PaymentMethod = (int) form.PaymentMethod,
            Note = form.Note
        });

        if (!result.Success)
            return Json(new { success = false, message = result.ErrorMessage });

        return Json(new { success = true });
    }

    /// <summary>
    /// Huỷ phiếu hoàn tiền (Pending → Cancelled). POST → redirect.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Cancel(Guid id)
    {
        await _mediator.Send(new CancelCustomerRefundCommand { Id = id });
        return RedirectToAction(nameof(Details), new { id });
    }
}
