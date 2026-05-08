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

    public async Task<IActionResult> Create()
    {
        var model = await _vendorReturnModelFactory.PrepareCreateVendorReturnModel();
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
            PurchaseOrderId = model.PurchaseOrderId,
            GoodsReceiptId = model.GoodsReceiptId,
            WarehouseId = model.WarehouseId!.Value,
            Note = model.Note,
            Items = model.Items.Select(i => new CreateVendorReturnItemCommand
            {
                ProductId = i.ProductId!.Value,
                GoodsReceiptItemId = i.GoodsReceiptItemId,
                RequestedQuantity = i.RequestedQuantity,
                AcceptedQuantity = i.AcceptedQuantity,
                UnitCost = i.UnitCost
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

    [HttpGet]
    public async Task<IActionResult> GetById(Guid id)
    {
        var model = await _mediator.Send(new GetVendorReturnQuery { Id = id });
        if (model is null)
            return Json(new { success = false });

        return Json(new { success = true, data = model });
    }
}
