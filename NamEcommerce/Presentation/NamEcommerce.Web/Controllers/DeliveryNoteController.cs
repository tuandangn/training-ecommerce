using MediatR;
using Microsoft.AspNetCore.Mvc;
using NamEcommerce.Application.Contracts.DeliveryNotes;
using NamEcommerce.Web.Constants;
using NamEcommerce.Web.Contracts.Commands.Models.DeliveryNotes;
using NamEcommerce.Web.Contracts.Models.DeliveryNotes;
using NamEcommerce.Web.Models.DeliveryNotes;
using NamEcommerce.Web.Services.DeliveryNotes;

namespace NamEcommerce.Web.Controllers;

public sealed class DeliveryNoteController : BaseAuthorizedController
{
    private readonly IDeliveryNoteModelFactory _deliveryNoteModelFactory;
    private readonly IMediator _mediator;
    private readonly IDeliveryNoteAppService _deliveryNoteAppService;

    public DeliveryNoteController(
        IDeliveryNoteModelFactory deliveryNoteModelFactory,
        IMediator mediator,
        IDeliveryNoteAppService deliveryNoteAppService)
    {
        _deliveryNoteModelFactory = deliveryNoteModelFactory;
        _mediator = mediator;
        _deliveryNoteAppService = deliveryNoteAppService;
    }

    public IActionResult Index() => RedirectToAction(nameof(List));

    public async Task<IActionResult> List(DeliveryNoteSearchModel searchModel)
    {
        var model = await _deliveryNoteModelFactory.PrepareDeliveryNoteListModelAsync(searchModel).ConfigureAwait(false);
        return View(model);
    }

    // Accept optional selected order item ids as comma separated list in query string
    public async Task<IActionResult> Create(Guid orderId, string? selected = null)
    {
        try
        {
            var model = await _deliveryNoteModelFactory.PrepareCreateDeliveryNoteModelAsync(orderId).ConfigureAwait(false);

            if (!string.IsNullOrEmpty(selected))
            {
                var ids = selected.Split([','], StringSplitOptions.RemoveEmptyEntries)
                                  .Select(s =>
                                  {
                                      if (Guid.TryParse(s, out var g)) return (Guid?)g; return null;
                                  })
                                  .Where(g => g.HasValue)
                                  .Select(g => g!.Value)
                                  .ToHashSet();

                if (ids.Any())
                {
                    // Set selection based on provided ids; default deselect those not included
                    foreach (var item in model.Items)
                    {
                        item.Selected = ids.Contains(item.OrderItemId);
                    }
                }
            }

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
            var refModel = await _deliveryNoteModelFactory.PrepareCreateDeliveryNoteModelAsync(model.OrderId).ConfigureAwait(false);
            model.OrderCode = refModel.OrderCode;
            model.CustomerName = refModel.CustomerName;
            return View(model);
        }

        var orderItems = model.Items.Where(i => i.Selected && i.Quantity > 0);
        if (!orderItems.Any())
        {
            ModelState.AddModelError(string.Empty, "Vui lòng chọn ít nhất một sản phẩm với số lượng hợp lệ.");
            var refModel = await _deliveryNoteModelFactory.PrepareCreateDeliveryNoteModelAsync(model.OrderId).ConfigureAwait(false);
            model.OrderCode = refModel.OrderCode;
            model.CustomerName = refModel.CustomerName;
            return View(model);
        }

        var result = await _mediator.Send(new CreateDeliveryNoteCommand
        {
            OrderId = model.OrderId,
            ShippingAddress = model.ShippingAddress,
            WarehouseId = model.WarehouseId,
            ShowPrice = model.ShowPrice,
            Note = model.Note,
            Surcharge = model.Surcharge,
            SurchargeReason = model.SurchargeReason,
            AmountToCollect = model.AmountToCollect,
            Items = orderItems.Select(i => new CreateDeliveryNoteCommand.CreateDeliveryNoteItemModel
            {
                OrderItemId = i.OrderItemId,
                Quantity = i.Quantity
            }).ToList()
        }).ConfigureAwait(false);

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

    [HttpPost]
    public async Task<IActionResult> CreateFromPreparation(
        [FromBody] CreateFromPreparationRequest request)
    {
        if (request == null || !request.SelectedItems.Any())
        {
            return Json(new { success = false, message = "Không có mặt hàng được chọn." });
        }

        try
        {
            var model = await _deliveryNoteModelFactory.PrepareCreateDeliveryNoteModelAsync(request.OrderId).ConfigureAwait(false);

            // Validate that requested quantities don't exceed remaining quantities
            foreach (var selectedItem in request.SelectedItems)
            {
                if (selectedItem.Quantity <= 0)
                    return Json(new { success = false, message = "Số lượng xuất phải lớn hơn 0." });

                var orderItem = model.Items.FirstOrDefault(i => i.OrderItemId == selectedItem.OrderItemId);
                if (orderItem == null)
                    return Json(new { success = false, message = $"Mặt hàng không được tìm thấy trong đơn hàng." });

                var remainingQty = orderItem.Quantity;
                if (selectedItem.Quantity > remainingQty)
                    return Json(new { success = false, message = $"Số lượng xuất vượt quá số lượng còn lại. {orderItem.ProductName}: Còn {remainingQty} để xuất." });
            }

            // Update items with user-selected quantities
            var selectedItemIds = request.SelectedItems.Select(s => s.OrderItemId).ToHashSet();
            foreach (var item in model.Items)
            {
                var selectedItem = request.SelectedItems.FirstOrDefault(s => s.OrderItemId == item.OrderItemId);
                if (selectedItem != null)
                {
                    item.Selected = true;
                    item.Quantity = selectedItem.Quantity;
                }
                else
                {
                    item.Selected = false;
                }
            }

            model.ShowPrice = request.ShowPrice;

            if (!string.IsNullOrEmpty(request.Note))
                model.Note = request.Note;

            var createResult = await _mediator.Send(new CreateDeliveryNoteCommand
            {
                OrderId = model.OrderId,
                Note = model.Note,
                ShowPrice = model.ShowPrice,
                WarehouseId = request.WarehouseId,
                ShippingAddress = model.ShippingAddress,
                Surcharge = request.Surcharge,
                SurchargeReason = request.SurchargeReason,
                AmountToCollect = request.AmountToCollect,
                Items = model.Items.Where(i => i.Selected).Select(i => new CreateDeliveryNoteCommand.CreateDeliveryNoteItemModel
                {
                    OrderItemId = i.OrderItemId,
                    Quantity = i.Quantity
                }).ToList()
            }).ConfigureAwait(false);

            if (createResult)
                return Json(new { success = true, message = "Tạo phiếu xuất kho thành công." });

            return Json(new { success = false, message = "Có lỗi xảy ra khi tạo phiếu xuất kho." });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "Lỗi: " + ex.Message });
        }
    }
}

public class CreateFromPreparationRequest
{
    public Guid OrderId { get; set; }
    public List<SelectedItemModel> SelectedItems { get; set; } = [];
    public bool ShowPrice { get; set; }
    public Guid WarehouseId { get; set; }
    public string? Note { get; set; }
    public decimal Surcharge { get; set; }
    public string? SurchargeReason { get; set; }
    public decimal AmountToCollect { get; set; }
}

public class SelectedItemModel
{
    public Guid OrderItemId { get; set; }
    public decimal Quantity { get; set; }
}
