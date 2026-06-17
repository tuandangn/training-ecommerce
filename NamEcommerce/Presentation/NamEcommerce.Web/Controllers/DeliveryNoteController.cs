using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NamEcommerce.Application.Contracts.DeliveryNotes;
using NamEcommerce.Domain.Shared.Exceptions;
using NamEcommerce.Web.Contracts.Commands.Models.DeliveryNotes;
using NamEcommerce.Web.Contracts.Models.DeliveryNotes;
using NamEcommerce.Web.Contracts.Queries.Models.Inventory;
using NamEcommerce.Web.Contracts.Queries.Models.Catalog;
using NamEcommerce.Web.Contracts.Queries.Models.Returns;
using NamEcommerce.Web.Contracts.Security;
using NamEcommerce.Web.Extensions;
using NamEcommerce.Web.Models.DeliveryNotes;
using NamEcommerce.Web.Services.CustomerPortal;
using NamEcommerce.Web.Services.DeliveryNotes;
using NamEcommerce.Domain.Shared.Services.Users;
using NamEcommerce.Domain.Shared.Common;
using System.Text.Json;

namespace NamEcommerce.Web.Controllers;

public sealed class DeliveryNoteController : BaseAuthorizedController
{
    private readonly IDeliveryNoteModelFactory _deliveryNoteModelFactory;
    private readonly IMediator _mediator;
    private readonly IDeliveryNoteAppService _deliveryNoteAppService;
    private readonly ICustomerPortalQrCodeService _customerPortalQrCodeService;
    private readonly ICurrentUserAccessor _currentUserAccessor;

    public DeliveryNoteController(
        IDeliveryNoteModelFactory deliveryNoteModelFactory,
        IMediator mediator,
        IDeliveryNoteAppService deliveryNoteAppService,
        ICustomerPortalQrCodeService customerPortalQrCodeService,
        ICurrentUserAccessor currentUserAccessor)
    {
        _deliveryNoteModelFactory = deliveryNoteModelFactory;
        _mediator = mediator;
        _deliveryNoteAppService = deliveryNoteAppService;
        _customerPortalQrCodeService = customerPortalQrCodeService;
        _currentUserAccessor = currentUserAccessor;
    }

    public IActionResult Index() => RedirectToAction(nameof(List));

    [Authorize(Policy = SystemPermissions.DeliveryNotes.View)]
    public async Task<IActionResult> List(DeliveryNoteSearchModel searchModel)
    {
        var model = await _deliveryNoteModelFactory.PrepareDeliveryNoteListModelAsync(searchModel).ConfigureAwait(false);
        return View(model);
    }

    [Authorize(Policy = SystemPermissions.DeliveryNotes.Manage)]
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
        catch (Exception)
        {
            NotifyError("Error.DeliveryNoteCreateFailed");
            return RedirectToAction("List", "Order");
        }
    }

    [HttpPost]
    [Authorize(Policy = SystemPermissions.DeliveryNotes.Manage)]
    public async Task<IActionResult> Create(CreateDeliveryNoteModel model)
    {
        if (!ModelState.IsValid)
        {
            model = await _deliveryNoteModelFactory.PrepareCreateDeliveryNoteModelAsync(model.OrderId, model).ConfigureAwait(false);
            return View(model);
        }

        var orderItems = BuildCreateDeliveryNoteItems(model);
        if (!orderItems.Any())
        {
            AddLocalizedModelError("Error.DeliveryNoteItemRequired");
            model = await _deliveryNoteModelFactory.PrepareCreateDeliveryNoteModelAsync(model.OrderId, model).ConfigureAwait(false);
            return View(model);
        }

        try
        {
            var result = await _mediator.Send(new CreateDeliveryNoteCommand
            {
                OrderId = model.OrderId,
                ShippingAddress = model.ShippingAddress,
                ShippingPhoneNumber = model.ShippingPhoneNumber,
                WarehouseId = model.WarehouseId,
                ShowPrice = model.ShowPrice,
                Note = model.Note,
                Surcharge = model.Surcharge,
                SurchargeReason = model.SurchargeReason,
                AmountToCollect = model.AmountToCollect,
                Items = orderItems
            }).ConfigureAwait(false);

            if (result.Success)
            {
                NotifySuccess(result.SuccessMessage ?? "Msg.SaveSuccess");
                return RedirectToAction(nameof(Details), new { id = result.CreatedId });
            }

            AddLocalizedModelError(result.ErrorMessage ?? "Error.DeliveryNoteCreateFailed");
            model = await _deliveryNoteModelFactory.PrepareCreateDeliveryNoteModelAsync(model.OrderId, model).ConfigureAwait(false);
            return View(model);
        }
        catch (NamEcommerceDomainException ex)
        {
            AddLocalizedModelError(ex.ErrorCode, ex.Parameters);
            model = await _deliveryNoteModelFactory.PrepareCreateDeliveryNoteModelAsync(model.OrderId, model).ConfigureAwait(false);
            return View(model);
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetCreateStockAvailability(Guid warehouseId, [FromQuery] Guid[] productIds)
    {
        if (productIds.Length == 0)
            return Json(new { items = Array.Empty<object>() });

        var distinctProductIds = productIds
            .Where(productId => productId != Guid.Empty)
            .Distinct()
            .ToList();

        var warehouses = (await _mediator.Send(new GetWarehouseOptionListQuery()).ConfigureAwait(false))
            .Options
            .ToList();

        var items = new List<object>(distinctProductIds.Count);
        foreach (var productId in distinctProductIds)
        {
            var warehouseItems = new List<object>(warehouses.Count);
            foreach (var warehouse in warehouses)
            {
                var stockInfo = await _mediator.Send(new GetProductStockInfoQuery(productId, warehouse.Id)).ConfigureAwait(false);
                warehouseItems.Add(new
                {
                    warehouseId = warehouse.Id,
                    warehouseName = warehouse.Name,
                    quantityAvailable = Math.Max(0m, stockInfo?.QuantityAvailable ?? 0m),
                    isDefault = warehouse.Id == warehouseId
                });
            }

            items.Add(new
            {
                productId,
                warehouses = warehouseItems
            });
        }

        return Json(new { items });
    }

    [Authorize(Policy = SystemPermissions.DeliveryNotes.View)]
    public async Task<IActionResult> Details(Guid id)
    {
        try
        {
            var model = await _deliveryNoteModelFactory.PrepareDeliveryNoteDetailsModelAsync(id).ConfigureAwait(false);
            return View(model);
        }
        catch
        {
            NotifyError("Error.DeliveryNoteNotFound");
            return RedirectToAction(nameof(List));
        }
    }

    [HttpPost]
    [Authorize(Policy = SystemPermissions.DeliveryNotes.Manage)]
    public async Task<IActionResult> AssignDeliveryUser(Guid id, Guid assignedDeliveryUserId)
    {
        var result = await _mediator.Send(new AssignDeliveryUserCommand
        {
            DeliveryNoteId = id,
            AssignedDeliveryUserId = assignedDeliveryUserId
        }).ConfigureAwait(false);

        if (result.Success)
            NotifySuccess(result.SuccessMessage ?? "Msg.SaveSuccess");
        else
            NotifyError(result.ErrorMessage ?? "Error.AssignDeliveryUserFailed");

        return RedirectToAction(nameof(Details), new { id });
    }

    [Authorize(Policy = SystemPermissions.DeliveryNotes.View)]
    public async Task<IActionResult> Print(Guid id)
    {
        try
        {
            var model = await _deliveryNoteModelFactory.PrepareDeliveryNoteDetailsModelAsync(id).ConfigureAwait(false);
            var qrCode = await _customerPortalQrCodeService.CreateDeliveryQrCodeAsync(id, Request).ConfigureAwait(false);
            if (qrCode is not null)
            {
                model.CustomerPortalUrl = qrCode.Url;
                model.CustomerPortalQrCodeSvg = qrCode.Svg;
            }

            return View(model);
        }
        catch
        {
            NotifyError("Error.DeliveryNoteNotFound");
            return RedirectToAction(nameof(List));
        }
    }

    [HttpPost]
    [Authorize(Policy = SystemPermissions.DeliveryNotes.Manage)]
    public async Task<IActionResult> Confirm(Guid id)
    {
        var result = await _mediator.Send(new ConfirmDeliveryNoteCommand
        {
            DeliveryNoteId = id
        }).ConfigureAwait(false);

        if (result.Success)
            return Json(new { success = true });

        return this.JsonError(LocalizeError(result.ErrorMessage!), data: new
        {
            shortageItems = result.ShortageItems,
            aggregationUrl = Url.Action("ShortageAggregation", "PurchaseOrder", new { deliveryNoteId = id })
        });
    }

    [HttpPost]
    [Authorize(Policy = SystemPermissions.DeliveryNotes.Manage)]
    public async Task<IActionResult> MarkDelivering(Guid id)
    {
        var deliveryNote = await _deliveryNoteAppService.GetByIdAsync(id).ConfigureAwait(false);
        if (deliveryNote is null)
            return this.JsonError(LocalizeError("Error.DeliveryNoteNotFound"));

        var result = await _mediator.Send(new MarkDeliveringDeliveryNoteCommand
        {
            DeliveryNoteId = id
        }).ConfigureAwait(false);

        if (!result.Success)
            return this.JsonError(LocalizeError(result.ErrorMessage ?? "Error.DeliveryNoteMarkDeliveringFailed"));

        return this.JsonOk();
    }

    [HttpPost]
    [Authorize(Policy = SystemPermissions.DeliveryNotes.Manage)]
    public async Task<IActionResult> MarkDelivered(
        Guid deliveryNoteId, string? receiverName, decimal agreedCustomerCharge,
        string? agreedCustomerChargeReason, bool compensateInNextDelivery,
        decimal? cashCollectedAmount, string? acceptanceItemsJson, [FromForm] IList<Guid>? pictureIds)
    {
        var deliveryNote = await _deliveryNoteAppService.GetByIdAsync(deliveryNoteId).ConfigureAwait(false);
        if (deliveryNote is null)
            return this.JsonError(LocalizeError("Error.DeliveryNoteNotFound"));

        if (pictureIds is null || pictureIds.Count == 0)
            return this.JsonError(message: LocalizeError("Error.DeliveryProofRequired"));

        var acceptanceItems = ParseAcceptanceItems(acceptanceItemsJson);
        var result = await _mediator.Send(new MarkDeliveryNoteDeliveredCommand
        {
            DeliveryNoteId = deliveryNoteId,
            ReceiverName = receiverName,
            AgreedCustomerCharge = agreedCustomerCharge,
            AgreedCustomerChargeReason = agreedCustomerChargeReason,
            CompensateInNextDelivery = compensateInNextDelivery,
            CashCollectedAmount = cashCollectedAmount,
            Items = acceptanceItems,
            PictureIds = pictureIds
        });

        if (result.Success)
            return this.JsonOk();

        return this.JsonError(LocalizeError(result.ErrorMessage!));
    }

    private static IList<MarkDeliveryNoteDeliveredItemCommand> ParseAcceptanceItems(string? acceptanceItemsJson)
    {
        if (string.IsNullOrWhiteSpace(acceptanceItemsJson))
            return [];

        try
        {
            var items = JsonSerializer.Deserialize<List<MarkDeliveryNoteDeliveredItemCommand>>(
                acceptanceItemsJson,
                new JsonSerializerOptions(JsonSerializerDefaults.Web));
            return items ?? [];
        }
        catch
        {
            return [];
        }
    }

    public async Task<IActionResult> GetDeliveryNoteAcceptantItemsInfo(Guid id)
    {
        var deliveryNote = await _deliveryNoteAppService.GetByIdAsync(id).ConfigureAwait(false);
        if (deliveryNote is null)
            return this.JsonError(LocalizeError("Error.DeliveryNoteNotFound"));

        var returnedQuantities = await _mediator.Send(new GetReturnedQuantitiesByDeliveryNoteQuery
        {
            DeliveryNoteId = id
        }).ConfigureAwait(false);

        var productIds = deliveryNote.Items.Select(i => i.ProductId).Distinct();
        var products = await _mediator.Send(new GetProductsByIdsForOrderQuery { Ids = productIds }).ConfigureAwait(false);
        var decimalPlacesByProductId = products.ToDictionary(p => p.Id, p => p.QuantityDecimalPlaces);

        var acceptantItems = deliveryNote.Items.Select(i =>
        {
            returnedQuantities.TryGetValue(i.Id, out var summary);
            return new DeliveryNoteItemModel
            {
                Id = i.Id,
                ProductName = i.ProductName,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                SubTotal = i.SubTotal,
                ReturnedQuantity = summary?.ConfirmedQuantity ?? 0m,
                PendingReturnQuantity = summary?.PendingQuantity ?? 0m,
                CompensatedReturnQuantity = summary?.ActiveCompensatedQuantity ?? 0m,
                QuantityDecimalPlaces = decimalPlacesByProductId.GetValueOrDefault(i.ProductId)
            };
        }).ToList();

        return this.JsonOk(new { acceptantItems, amountToCollect = deliveryNote.AmountToCollect });
    }

    [HttpPost]
    [Authorize(Policy = SystemPermissions.DeliveryNotes.Manage)]
    public async Task<IActionResult> ApproveSettlement(
        Guid deliveryNoteId, decimal approvedAmountToCollect,
        decimal agreedCustomerCharge, string? agreedCustomerChargeReason, string? adminNote)
    {
        if (!User.IsInRole(SystemUserRoleNames.Admin))
            return Forbid();

        var currentUser = await _currentUserAccessor.GetCurrentUserAsync().ConfigureAwait(false);
        var result = await _mediator.Send(new ApproveDeliverySettlementCommand
        {
            DeliveryNoteId = deliveryNoteId,
            ApprovedAmountToCollect = approvedAmountToCollect,
            AgreedCustomerCharge = agreedCustomerCharge,
            AgreedCustomerChargeReason = agreedCustomerChargeReason,
            AdminNote = adminNote,
            ApprovedByUserId = currentUser?.Id
        }).ConfigureAwait(false);

        if (result.Success)
            NotifySuccess(result.SuccessMessage ?? "Msg.SaveSuccess");
        else
            NotifyError(result.ErrorMessage ?? "Error.GenericError");

        return RedirectToAction(nameof(Details), new { id = deliveryNoteId });
    }

    [HttpPost]
    [Authorize(Policy = SystemPermissions.DeliveryNotes.Manage)]
    public async Task<IActionResult> RejectSettlement(Guid deliveryNoteId, string reason)
    {
        if (!User.IsInRole(SystemUserRoleNames.Admin))
            return Forbid();

        var currentUser = await _currentUserAccessor.GetCurrentUserAsync().ConfigureAwait(false);
        var result = await _mediator.Send(new RejectDeliverySettlementCommand
        {
            DeliveryNoteId = deliveryNoteId,
            Reason = reason,
            ApprovedByUserId = currentUser?.Id
        }).ConfigureAwait(false);

        if (result.Success)
            NotifySuccess(result.SuccessMessage ?? "Msg.SaveSuccess");
        else
            NotifyError(result.ErrorMessage ?? "Error.GenericError");

        return RedirectToAction(nameof(Details), new { id = deliveryNoteId });
    }

    [HttpPost]
    [Authorize(Policy = SystemPermissions.DeliveryNotes.Manage)]
    public async Task<IActionResult> Cancel(Guid id)
    {
        try
        {
            await _mediator.Send(new CancelDeliveryNoteCommand
            {
                DeliveryNoteId = id
            }).ConfigureAwait(false);

            return Json(new { success = true, message = LocalizeError("Msg.SaveSuccess") });
        }
        catch (NamEcommerceDomainException ex)
        {
            return Json(new { success = false, message = LocalizeError(ex.ErrorCode, ex.Parameters) });
        }
    }

    [HttpPost]
    [Authorize(Policy = SystemPermissions.DeliveryNotes.Manage)]
    public async Task<IActionResult> CreateFromPreparation([FromBody] CreateFromPreparationRequest request)
    {
        if (request == null || !request.SelectedItems.Any())
        {
            return Json(new { success = false, message = LocalizeError("Error.DeliveryNoteItemRequired") });
        }

        try
        {
            var model = await _deliveryNoteModelFactory.PrepareCreateDeliveryNoteModelAsync(request.OrderId).ConfigureAwait(false);

            foreach (var selectedItem in request.SelectedItems)
            {
                if (selectedItem.Quantity <= 0)
                    return Json(new { success = false, message = LocalizeError("Error.OrderItemQuantityMustBePositive") });
            }

            // Validate that requested quantities don't exceed remaining quantities
            foreach (var selectedGroup in request.SelectedItems.GroupBy(item => item.OrderItemId))
            {
                var orderItem = model.Items.FirstOrDefault(i => i.OrderItemId == selectedGroup.Key);
                if (orderItem == null)
                    return Json(new { success = false, message = LocalizeError("Error.OrderItemIsNotFound") });

                var remainingQty = orderItem.Quantity;
                var requestedQuantity = selectedGroup.Sum(item => item.Quantity);
                if (requestedQuantity > remainingQty)
                    return Json(new { success = false, message = LocalizeError("Error.QuantityExceedsRemaining", orderItem.ProductName, remainingQty) });
            }

            // Update items with user-selected quantities
            var selectedItemIds = request.SelectedItems.Select(s => s.OrderItemId).ToHashSet();
            foreach (var item in model.Items)
            {
                var selectedItem = request.SelectedItems.FirstOrDefault(s => s.OrderItemId == item.OrderItemId);
                if (selectedItem != null)
                {
                    item.Selected = true;
                    item.Quantity = request.SelectedItems
                        .Where(s => s.OrderItemId == item.OrderItemId)
                        .Sum(s => s.Quantity);
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
                ShippingPhoneNumber = model.ShippingPhoneNumber,
                Surcharge = request.Surcharge,
                SurchargeReason = request.SurchargeReason,
                AmountToCollect = request.AmountToCollect,
                Items = request.SelectedItems.Select(selectedItem => new CreateDeliveryNoteCommand.CreateDeliveryNoteItemModel
                {
                    OrderItemId = selectedItem.OrderItemId,
                    WarehouseId = selectedItem.WarehouseId == Guid.Empty ? request.WarehouseId : selectedItem.WarehouseId,
                    Quantity = selectedItem.Quantity
                }).ToList()
            }).ConfigureAwait(false);

            if (createResult.Success)
            {
                return Json(new
                {
                    success = true,
                    message = LocalizeError(createResult.SuccessMessage ?? "Msg.SaveSuccess")
                });
            }

            return Json(new
            {
                success = false,
                message = LocalizeError(createResult.ErrorMessage ?? "Error.DeliveryNoteCreateFailed")
            });
        }
        catch (NamEcommerceDomainException ex)
        {
            return Json(new
            {
                success = false,
                message = LocalizeError(ex.ErrorCode, ex.Parameters)
            });
        }
        catch
        {
            return Json(new
            {
                success = false,
                message = LocalizeError("Error.GenericError")
            });
        }
    }

    private static IList<CreateDeliveryNoteCommand.CreateDeliveryNoteItemModel> BuildCreateDeliveryNoteItems(CreateDeliveryNoteModel model)
    {
        var result = new List<CreateDeliveryNoteCommand.CreateDeliveryNoteItemModel>();
        foreach (var item in model.Items.Where(i => i.Selected))
        {
            if (item.WarehouseAllocations.Any())
            {
                result.AddRange(item.WarehouseAllocations
                    .Where(allocation => allocation.WarehouseId != Guid.Empty && allocation.Quantity > 0)
                    .Select(allocation => new CreateDeliveryNoteCommand.CreateDeliveryNoteItemModel
                    {
                        OrderItemId = item.OrderItemId,
                        WarehouseId = allocation.WarehouseId,
                        Quantity = allocation.Quantity
                    }));
                continue;
            }

            var warehouseId = item.WarehouseId == Guid.Empty ? model.WarehouseId : item.WarehouseId;
            if (warehouseId == Guid.Empty || item.Quantity <= 0)
                continue;

            result.Add(new CreateDeliveryNoteCommand.CreateDeliveryNoteItemModel
            {
                OrderItemId = item.OrderItemId,
                WarehouseId = warehouseId,
                Quantity = item.Quantity
            });
        }

        return result;
    }

    [HttpPost]
    [Authorize(Policy = SystemPermissions.DeliveryNotes.Manage)]
    public async Task<IActionResult> UpdateShipping(Guid deliveryNoteId, string? shippingAddress, string? shippingPhoneNumber)
    {
        var result = await _mediator.Send(new UpdateDeliveryNoteShippingCommand(
            deliveryNoteId,
            shippingAddress,
            shippingPhoneNumber)).ConfigureAwait(false);

        if (result.Success)
            NotifySuccess("Msg.SaveSuccess");
        else
            NotifyError(result.ErrorMessage ?? "Error.DeliveryNoteCannotUpdateShipping");

        return RedirectToAction(nameof(Details), new { id = deliveryNoteId });
    }
}

#region Helper classes

public class CreateFromPreparationRequest
{
    public Guid OrderId { get; set; }
    public List<SelectedItemModel> SelectedItems { get; set; } = [];
    public bool ShowPrice { get; set; }
    public bool CompensateReturnedQuantityInNextDelivery { get; set; }
    public Guid WarehouseId { get; set; }
    public string? Note { get; set; }
    public decimal Surcharge { get; set; }
    public string? SurchargeReason { get; set; }
    public decimal AmountToCollect { get; set; }
}

public class SelectedItemModel
{
    public Guid OrderItemId { get; set; }
    public Guid WarehouseId { get; set; }
    public decimal Quantity { get; set; }
}

#endregion 
