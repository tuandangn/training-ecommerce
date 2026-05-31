using MediatR;
using Microsoft.AspNetCore.Mvc;
using NamEcommerce.Application.Contracts.Dtos.PurchaseOrders;
using NamEcommerce.Application.Contracts.PurchaseOrders;
using NamEcommerce.Web.Contracts.Commands.Models.PurchaseOrders;
using NamEcommerce.Web.Contracts.Queries.Models.Inventory;
using NamEcommerce.Web.Models.DirectShipDelivery;

namespace NamEcommerce.Web.Controllers;

public sealed class DirectShipDeliveryController(
    IMediator mediator, IDirectShipAppService directShipAppService) : BaseAuthorizedController
{
    public IActionResult Index() => RedirectToAction(nameof(Pending));

    public async Task<IActionResult> Pending(DirectShipDeliveryFilterModel filter)
    {
        var deliveryNotes = await directShipAppService.GetPendingDeliveriesAsync(new PendingDirectShipFilterAppDto
        {
            Keywords = filter.Keywords,
            FromDateUtc = filter.FromDate.HasValue ? filter.FromDate.Value.ToUniversalTime() : null,
            ToDateUtc = filter.ToDate.HasValue ? filter.ToDate.Value.Date.AddDays(1).AddSeconds(-1).ToUniversalTime() : null
        }).ConfigureAwait(false);

        var model = new PendingDirectShipDeliveryListModel
        {
            Filter = filter,
            Items = deliveryNotes.Select(deliveryNote => new PendingDirectShipDeliveryModel
            {
                Id = deliveryNote.Id,
                WarehouseId = deliveryNote.WarehouseId,
                Code = deliveryNote.Code,
                OrderId = deliveryNote.OrderId,
                OrderCode = deliveryNote.OrderCode,
                CustomerName = deliveryNote.CustomerName,
                CustomerPhone = deliveryNote.CustomerPhone,
                ShippingAddress = deliveryNote.ShippingAddress,
                CreatedOn = deliveryNote.CreatedOnUtc.ToLocalTime(),
                Items = deliveryNote.Items.Select(i => new PendingDirectShipDeliveryItemModel
                {
                    ProductName = i.ProductName,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice
                }).ToList()
            }).ToList(),
            ReturnWarehouseOptions = await mediator.Send(new GetWarehouseOptionListQuery()).ConfigureAwait(false)
        };

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> ConfirmDelivery([FromBody] ConfirmDirectShipDeliveryRequest request)
    {
        if (request.DeliveryNoteId == Guid.Empty)
            return Json(new { success = false, message = "ID phiếu không hợp lệ." });

        var confirmedAt = request.ConfirmedDate.HasValue
            ? request.ConfirmedDate.Value.ToUniversalTime()
            : DateTime.UtcNow;

        var result = await mediator.Send(new ConfirmDirectShipDeliveryCommand
        {
            DeliveryNoteId = request.DeliveryNoteId,
            ConfirmedAtUtc = confirmedAt,
            Note = request.Note
        }).ConfigureAwait(false);

        if (result.Success)
            return Json(new { success = true, message = LocalizeError("Msg.SaveSuccess") });

        return Json(new { success = false, message = LocalizeError(result.ErrorMessage!) });
    }

    [HttpPost]
    public async Task<IActionResult> RejectDelivery(RejectDirectShipDeliveryRequest request)
    {
        if (request.DeliveryNoteId == Guid.Empty)
            return Json(new { success = false, message = "ID phiếu không hợp lệ." });

        if (string.IsNullOrWhiteSpace(request.Reason))
            return Json(new { success = false, message = "Lý do từ chối là bắt buộc." });

        if (request.ReturnWarehouseId == Guid.Empty)
            return Json(new { success = false, message = "Vui lòng chọn kho nhận hàng trả về." });

        var result = await mediator.Send(new RejectDirectShipDeliveryCommand
        {
            DeliveryNoteId = request.DeliveryNoteId,
            ReturnWarehouseId = request.ReturnWarehouseId,
            Reason = request.Reason
        }).ConfigureAwait(false);

        if (result.Success)
            return Json(new { success = true, message = LocalizeError("Msg.SaveSuccess") });

        return Json(new { success = false, message = LocalizeError(result.ErrorMessage!) });
    }

    [HttpPost]
    public async Task<IActionResult> MarkAsDirectShip([FromBody] MarkAllocationAsDirectShipRequest request)
    {
        if (request.AllocationId == Guid.Empty || string.IsNullOrWhiteSpace(request.Address))
            return Json(new { success = false, message = "Dữ liệu không hợp lệ." });

        var result = await mediator.Send(new MarkAllocationAsDirectShipCommand
        {
            AllocationId = request.AllocationId,
            Address = request.Address,
            ContactName = request.ContactName,
            ContactPhone = request.ContactPhone
        }).ConfigureAwait(false);

        if (result.Success)
            return Json(new { success = true });

        return Json(new { success = false, message = LocalizeError(result.ErrorMessage!) });
    }

    [HttpPost]
    public async Task<IActionResult> UpdateAddress([FromBody] UpdateDirectShipAddressRequest request)
    {
        if (request.AllocationId == Guid.Empty || string.IsNullOrWhiteSpace(request.NewAddress))
            return Json(new { success = false, message = "Dữ liệu không hợp lệ." });

        var result = await mediator.Send(new UpdateDirectShipAddressCommand
        {
            AllocationId = request.AllocationId,
            NewAddress = request.NewAddress,
            NewContactName = request.NewContactName,
            NewContactPhone = request.NewContactPhone,
            Reason = request.Reason
        }).ConfigureAwait(false);

        if (result.Success)
            return Json(new { success = true });

        return Json(new { success = false, message = result.ErrorMessage });
    }
}

public sealed class ConfirmDirectShipDeliveryRequest
{
    public Guid DeliveryNoteId { get; set; }
    public DateTime? ConfirmedDate { get; set; }
    public string? Note { get; set; }
}

public sealed class RejectDirectShipDeliveryRequest
{
    public Guid DeliveryNoteId { get; set; }
    public Guid ReturnWarehouseId { get; set; }
    public required string Reason { get; set; }
}

public sealed class UpdateDirectShipAddressRequest
{
    public Guid AllocationId { get; set; }
    public required string NewAddress { get; set; }
    public string? NewContactName { get; set; }
    public string? NewContactPhone { get; set; }
    public string? Reason { get; set; }
}

public sealed class MarkAllocationAsDirectShipRequest
{
    public Guid AllocationId { get; set; }
    public required string Address { get; set; }
    public string? ContactName { get; set; }
    public string? ContactPhone { get; set; }
}
