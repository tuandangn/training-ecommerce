using MediatR;
using Microsoft.AspNetCore.Mvc;
using NamEcommerce.Web.Contracts.Commands.Models.Returns;
using NamEcommerce.Web.Contracts.Queries.Models.Returns;
using NamEcommerce.Web.Models.Returns;
using NamEcommerce.Web.Services.Returns;

namespace NamEcommerce.Web.Controllers;

public sealed class CustomerReturnController : BaseAuthorizedController
{
    private readonly IMediator _mediator;
    private readonly ICustomerReturnModelFactory _customerReturnModelFactory;

    public CustomerReturnController(IMediator mediator, ICustomerReturnModelFactory customerReturnModelFactory)
    {
        _mediator = mediator;
        _customerReturnModelFactory = customerReturnModelFactory;
    }

    public IActionResult Index() => RedirectToAction(nameof(List));

    public async Task<IActionResult> List(CustomerReturnListSearchModel search)
    {
        var model = await _customerReturnModelFactory.PrepareCustomerReturnListModel(search);
        return View(model);
    }

    public async Task<IActionResult> Create()
    {
        var model = await _customerReturnModelFactory.PrepareCreateCustomerReturnModel();
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateCustomerReturnModel model)
    {
        if (!ModelState.IsValid)
        {
            model = await _customerReturnModelFactory.PrepareCreateCustomerReturnModel(model);
            return View(model);
        }

        var result = await _mediator.Send(new CreateCustomerReturnCommand
        {
            OrderId = model.OrderId!.Value,
            WarehouseId = model.WarehouseId!.Value,
            Note = model.Note,
            Items = model.Items.Select(i => new CreateCustomerReturnItemCommand
            {
                ProductId = i.ProductId!.Value,
                DeliveryNoteItemId = i.DeliveryNoteItemId,
                RequestedQuantity = i.RequestedQuantity,
                AcceptedQuantity = i.AcceptedQuantity,
                UnitPrice = i.UnitPrice
            }).ToList()
        });

        if (!result.Success)
        {
            AddLocalizedModelError(result.ErrorMessage);
            model = await _customerReturnModelFactory.PrepareCreateCustomerReturnModel(model);
            return View(model);
        }

        NotifySuccess("Msg.SaveSuccess");
        return RedirectToAction(nameof(Details), new { id = result.CreatedId });
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var model = await _customerReturnModelFactory.PrepareCustomerReturnDetailsModel(id);
        if (model is null)
        {
            NotifyError("Error.CustomerReturn.IsNotFound");
            return RedirectToAction(nameof(List));
        }

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Update(Guid id, string? note, DateTime? returnDate)
    {
        var result = await _mediator.Send(new UpdateCustomerReturnCommand
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
        var result = await _mediator.Send(new MoveCustomerReturnToInspectingCommand { Id = id });

        if (!result.Success)
            return Json(new { success = false, message = LocalizeError(result.ErrorMessage!) });

        return Json(new { success = true });
    }

    [HttpPost]
    public async Task<IActionResult> Confirm(Guid id)
    {
        var result = await _mediator.Send(new ConfirmCustomerReturnCommand { Id = id });

        if (!result.Success)
            return Json(new { success = false, message = LocalizeError(result.ErrorMessage!) });

        return Json(new { success = true });
    }

    [HttpPost]
    public async Task<IActionResult> Cancel(Guid id)
    {
        var (success, errorMessage) = await _mediator.Send(new CancelCustomerReturnCommand { Id = id });

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

    [HttpGet]
    public async Task<IActionResult> GetById(Guid id)
    {
        var model = await _mediator.Send(new GetCustomerReturnQuery { Id = id });
        if (model is null)
            return Json(new { success = false });

        return Json(new { success = true, data = model });
    }
}
