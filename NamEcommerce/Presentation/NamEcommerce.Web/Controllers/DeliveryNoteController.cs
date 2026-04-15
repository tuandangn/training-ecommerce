using MediatR;
using Microsoft.AspNetCore.Mvc;
using NamEcommerce.Web.Constants;
using NamEcommerce.Web.Contracts.Commands.Models.DeliveryNotes;
using NamEcommerce.Web.Contracts.Models.DeliveryNotes;
using NamEcommerce.Web.Contracts.Services;

namespace NamEcommerce.Web.Controllers;

public sealed class DeliveryNoteController : BaseAuthorizedController
{
    private readonly IDeliveryNoteModelFactory _deliveryNoteModelFactory;
    private readonly IMediator _mediator;

    public DeliveryNoteController(
        IDeliveryNoteModelFactory deliveryNoteModelFactory,
        IMediator mediator)
    {
        _deliveryNoteModelFactory = deliveryNoteModelFactory;
        _mediator = mediator;
    }

    public IActionResult Index() => RedirectToAction(nameof(List));

    public async Task<IActionResult> List(DeliveryNoteSearchModel searchModel)
    {
        var model = await _deliveryNoteModelFactory.PrepareDeliveryNoteListModelAsync(searchModel).ConfigureAwait(false);
        return View(model);
    }

    public async Task<IActionResult> Create(Guid orderId)
    {
        try
        {
            var model = await _deliveryNoteModelFactory.PrepareCreateDeliveryNoteModelAsync(orderId).ConfigureAwait(false);
            return View(model);
        }
        catch (Exception ex)
        {
            TempData[ViewConstants.CustomerErrorMessage] = "Lỗi khi tạo phiếu xuất: " + ex.Message;
            return RedirectToAction("List", "Order");
        }
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateDeliveryNoteModel model)
    {
        if (!ModelState.IsValid)
        {
            // Re-populate order details in case of validation failure
            var refModel = await _deliveryNoteModelFactory.PrepareCreateDeliveryNoteModelAsync(model.OrderId).ConfigureAwait(false);
            model.OrderCode = refModel.OrderCode;
            model.CustomerName = refModel.CustomerName;
            return View(model);
        }

        var result = await _mediator.Send(new CreateDeliveryNoteCommand { Model = model }).ConfigureAwait(false);
        if (result)
        {
            TempData[ViewConstants.CustomerSuccessMessage] = "Tạo phiếu xuất kho thành công.";
            return RedirectToAction(nameof(List));
        }

        ModelState.AddModelError(string.Empty, "Có lỗi xảy ra khi tạo phiếu xuất kho.");
        var refModel2 = await _deliveryNoteModelFactory.PrepareCreateDeliveryNoteModelAsync(model.OrderId).ConfigureAwait(false);
        model.OrderCode = refModel2.OrderCode;
        model.CustomerName = refModel2.CustomerName;
        return View(model);
    }

    public async Task<IActionResult> Details(Guid id)
    {
        try
        {
            var model = await _deliveryNoteModelFactory.PrepareDeliveryNoteDetailsModelAsync(id).ConfigureAwait(false);
            return View(model);
        }
        catch
        {
            TempData[ViewConstants.CustomerErrorMessage] = "Không tìm thấy phiếu xuất kho.";
            return RedirectToAction(nameof(List));
        }
    }

    public async Task<IActionResult> Print(Guid id)
    {
        try
        {
            var model = await _deliveryNoteModelFactory.PrepareDeliveryNoteDetailsModelAsync(id).ConfigureAwait(false);
            return View(model);
        }
        catch
        {
            TempData[ViewConstants.CustomerErrorMessage] = "Không tìm thấy phiếu xuất kho.";
            return RedirectToAction(nameof(List));
        }
    }

    [HttpPost]
    public async Task<IActionResult> Confirm(Guid id)
    {
        var result = await _mediator.Send(new ConfirmDeliveryNoteCommand
        {
            DeliveryNoteId = id
        }).ConfigureAwait(false);

        if (result.Success)
            return Json(new { success = true, message = "Đã xác nhận phiếu xuất." });

        return Json(new { success = false, message = result.ErrorMessage });
    }

    [HttpPost]
    public async Task<IActionResult> MarkDelivering(Guid id)
    {
        var result = await _mediator.Send(new MarkDeliveringDeliveryNoteCommand
        {
            DeliveryNoteId = id
        }).ConfigureAwait(false);

        if (result.Success)
            return Json(new { success = true, message = "Đang giao hàng." });

        return Json(new { success = false, message = result.ErrorMessage });
    }

    [HttpPost]
    public async Task<IActionResult> MarkDelivered(Guid deliveryNoteId, string? receiverName, IFormFile? pictureFile)
    {
        if (pictureFile == null || pictureFile.Length == 0)
        {
            return Json(new { success = false, message = "Vui lòng chụp ảnh bằng chứng giao hàng." });
        }

        using var memoryStream = new MemoryStream();
        await pictureFile.CopyToAsync(memoryStream);
        var fileBytes = memoryStream.ToArray();

        var result = await _mediator.Send(new MarkDeliveryNoteDeliveredCommand
        {
            DeliveryNoteId = deliveryNoteId,
            ReceiverName = receiverName,
            PictureData = fileBytes,
            PictureContentType = pictureFile.ContentType,
            PictureFileName = pictureFile.FileName
        });

        if (result.Success)
        {
            return Json(new { success = true, message = "Đã giao hàng thành công." });
        }

        return Json(new { success = false, message = result.ErrorMessage });
    }

    [HttpPost]
    public async Task<IActionResult> Cancel(Guid id)
    {
        var result = await _mediator.Send(new CancelDeliveryNoteCommand
        {
            DeliveryNoteId = id
        }).ConfigureAwait(false);

        if (result.Success)
            return Json(new { success = true, message = "Đã hủy phiếu xuất kho." });

        return Json(new { success = false, message = result.ErrorMessage });
    }
}
