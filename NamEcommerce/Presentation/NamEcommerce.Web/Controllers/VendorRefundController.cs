using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NamEcommerce.Web.Contracts.Commands.Models.Debts;
using NamEcommerce.Web.Contracts.Security;
using NamEcommerce.Web.Models.Debts;
using NamEcommerce.Web.Services.Debts;

namespace NamEcommerce.Web.Controllers;

[Authorize(Policy = SystemPermissions.Debts.VendorRefundsManage)]
public sealed class VendorRefundController : BaseAuthorizedController
{
    private readonly IMediator _mediator;
    private readonly IVendorRefundModelFactory _modelFactory;

    public VendorRefundController(IMediator mediator, IVendorRefundModelFactory modelFactory)
    {
        _mediator = mediator;
        _modelFactory = modelFactory;
    }

    public IActionResult Index() => RedirectToAction(nameof(List));

    public async Task<IActionResult> List(VendorRefundListSearchModel search)
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

    [HttpPost]
    public async Task<IActionResult> Complete(Guid id, [FromBody] CompleteVendorRefundModel form)
    {
        var result = await _mediator.Send(new CompleteVendorRefundCommand
        {
            Id = id,
            PaymentMethod = (int)form.PaymentMethod,
            Note = form.Note
        });

        if (!result.Success)
            return Json(new { success = false, message = result.ErrorMessage });

        return Json(new { success = true });
    }

    [HttpPost]
    public async Task<IActionResult> Cancel(Guid id)
    {
        await _mediator.Send(new CancelVendorRefundCommand { Id = id });
        return RedirectToAction(nameof(Details), new { id });
    }
}
