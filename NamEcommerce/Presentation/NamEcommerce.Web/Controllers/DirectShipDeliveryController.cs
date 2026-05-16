using Microsoft.AspNetCore.Mvc;
using NamEcommerce.Application.Contracts.Dtos.PurchaseOrders;
using NamEcommerce.Application.Contracts.PurchaseOrders;
using NamEcommerce.Web.Models.DirectShipDelivery;

namespace NamEcommerce.Web.Controllers;

public sealed class DirectShipDeliveryController(IDirectShipAppService directShipAppService) : BaseAuthorizedController
{
    public IActionResult Index() => RedirectToAction(nameof(Pending));

    public async Task<IActionResult> Pending(DirectShipDeliveryFilterModel filter)
    {
        var items = await directShipAppService.GetPendingDeliveriesAsync(new PendingDirectShipFilterAppDto
        {
            Keywords = filter.Keywords,
            FromDateUtc = filter.FromDate.HasValue ? filter.FromDate.Value.ToUniversalTime() : null,
            ToDateUtc = filter.ToDate.HasValue ? filter.ToDate.Value.Date.AddDays(1).AddSeconds(-1).ToUniversalTime() : null
        }).ConfigureAwait(false);

        var model = new PendingDirectShipDeliveryListModel
        {
            Filter = filter,
            Items = items.Select(d => new PendingDirectShipDeliveryModel
            {
                Id = d.Id,
                Code = d.Code,
                OrderId = d.OrderId,
                OrderCode = d.OrderCode,
                CustomerName = d.CustomerName,
                CustomerPhone = d.CustomerPhone,
                ShippingAddress = d.ShippingAddress,
                CreatedOn = d.CreatedOnUtc.ToLocalTime(),
                Items = d.Items.Select(i => new PendingDirectShipDeliveryItemModel
                {
                    ProductName = i.ProductName,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice
                }).ToList()
            }).ToList()
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

        var result = await directShipAppService.ConfirmDeliveryAsync(new ConfirmDirectShipDeliveryAppDto
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
    public async Task<IActionResult> RejectDelivery([FromBody] RejectDirectShipDeliveryRequest request)
    {
        if (request.DeliveryNoteId == Guid.Empty)
            return Json(new { success = false, message = "ID phiếu không hợp lệ." });

        if (string.IsNullOrWhiteSpace(request.Reason))
            return Json(new { success = false, message = "Lý do từ chối là bắt buộc." });

        var result = await directShipAppService.RejectDeliveryAsync(new RejectDirectShipDeliveryAppDto
        {
            DeliveryNoteId = request.DeliveryNoteId,
            Reason = request.Reason
        }).ConfigureAwait(false);

        if (result.Success)
            return Json(new { success = true, message = LocalizeError("Msg.SaveSuccess") });

        return Json(new { success = false, message = LocalizeError(result.ErrorMessage!) });
    }

    [HttpPost]
    public async Task<IActionResult> UpdateAddress([FromBody] UpdateDirectShipAddressRequest request)
    {
        if (request.AllocationId == Guid.Empty || string.IsNullOrWhiteSpace(request.NewAddress))
            return Json(new { success = false, message = "Dữ liệu không hợp lệ." });

        var result = await directShipAppService.UpdateDirectShipAddressAsync(new UpdateDirectShipAddressAppDto
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
