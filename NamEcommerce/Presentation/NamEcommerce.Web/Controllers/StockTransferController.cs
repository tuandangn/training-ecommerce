using MediatR;
using Microsoft.AspNetCore.Mvc;
using NamEcommerce.Web.Contracts.Commands.StockTransfer;
using NamEcommerce.Web.Models.StockTransfer;
using NamEcommerce.Web.Services.StockTransfer;

namespace NamEcommerce.Web.Controllers;

public sealed class StockTransferController : BaseAuthorizedController
{
    private readonly IMediator _mediator;
    private readonly IStockTransferNoteModelFactory _modelFactory;

    public StockTransferController(IMediator mediator, IStockTransferNoteModelFactory modelFactory)
    {
        _mediator = mediator;
        _modelFactory = modelFactory;
    }

    public IActionResult Index() => RedirectToAction(nameof(List));

    public async Task<IActionResult> List(int pageNumber = 1, int pageSize = 20,
        string? keywords = null, Guid? fromWarehouseId = null, int? status = null)
    {
        var model = await _modelFactory.PrepareListModelAsync(pageNumber, pageSize, keywords, fromWarehouseId, status);
        return View(model);
    }

    public async Task<IActionResult> Create()
    {
        var model = await _modelFactory.PrepareCreateModelAsync();
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateStockTransferNoteModel model)
    {
        if (!ModelState.IsValid)
        {
            var refreshed = await _modelFactory.PrepareCreateModelAsync();
            model.AvailableWarehouses = refreshed.AvailableWarehouses;
            return View(model);
        }

        var result = await _mediator.Send(new CreateStockTransferNoteCommand(
            model.FromWarehouseId!.Value,
            model.ToWarehouseId!.Value,
            model.Note,
            model.Items
        ));

        if (!result.Success)
        {
            AddLocalizedModelError(result.ErrorMessage);
            var refreshed = await _modelFactory.PrepareCreateModelAsync();
            model.AvailableWarehouses = refreshed.AvailableWarehouses;
            return View(model);
        }

        NotifySuccess("Msg.SaveSuccess");
        return RedirectToAction(nameof(Details), new { id = result.CreatedId });
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var model = await _modelFactory.PrepareDetailsModelAsync(id);
        if (model is null)
        {
            NotifyError("Không tìm thấy phiếu chuyển kho.");
            return RedirectToAction(nameof(List));
        }

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Approve(Guid id)
    {
        var result = await _mediator.Send(new ApproveStockTransferNoteCommand(id));

        if (!result.Success)
            return Json(new { success = false, message = LocalizeError(result.ErrorMessage!) });

        return Json(new { success = true });
    }

    [HttpPost]
    public async Task<IActionResult> Cancel(Guid id)
    {
        var result = await _mediator.Send(new CancelStockTransferNoteCommand(id));

        if (!result.Success)
            NotifyError(result.ErrorMessage!);
        else
            NotifySuccess("Msg.CancelSuccess");

        return RedirectToAction(nameof(Details), new { id });
    }
}
