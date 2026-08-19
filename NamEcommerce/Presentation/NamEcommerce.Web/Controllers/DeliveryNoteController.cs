using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NamEcommerce.Application.Contracts.DeliveryNotes;
using NamEcommerce.Application.Contracts.Dtos.DeliveryNotes;
using NamEcommerce.Application.Contracts.Orders;
using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Domain.Shared.Enums.DeliveryNotes;
using NamEcommerce.Domain.Shared.Exceptions;
using NamEcommerce.Domain.Shared.Services.Users;
using NamEcommerce.Web.Contracts.Commands.Models.DeliveryNotes;
using NamEcommerce.Web.Contracts.Models.DeliveryNotes;
using NamEcommerce.Web.Contracts.Queries.Models.Catalog;
using NamEcommerce.Web.Contracts.Queries.Models.Inventory;
using NamEcommerce.Web.Contracts.Queries.Models.Orders;
using NamEcommerce.Web.Contracts.Queries.Models.Returns;
using NamEcommerce.Web.Contracts.Security;
using NamEcommerce.Web.Extensions;
using NamEcommerce.Web.Models.DeliveryNotes;
using NamEcommerce.Web.Services.Common;
using NamEcommerce.Web.Services.DeliveryNotes;
using System.Text.Json;

namespace NamEcommerce.Web.Controllers;

public sealed class DeliveryNoteController(
    IDeliveryNoteModelFactory deliveryNoteModelFactory,
    IMediator mediator, IConfiguration configuration,
    IDeliveryNoteAppService deliveryNoteAppService,
    ICurrentUserAccessor currentUserAccessor,
    ICachedValuesService cachedValuesService,
    IOrderAppService orderAppService) : BaseAuthorizedController
{
    public IActionResult Index() => RedirectToAction(nameof(List));

    [Authorize(Policy = SystemPermissions.DeliveryNotes.View)]
    public async Task<IActionResult> List(DeliveryNoteListSearchModel searchModel)
    {
        var model = await deliveryNoteModelFactory.PrepareDeliveryNoteListModelAsync(searchModel);
        return View(model);
    }

    [Authorize(Policy = SystemPermissions.DeliveryNotes.Manage)]
    public async Task<IActionResult> Create(Guid orderId, string? selected = null)
    {
        var order = await orderAppService.GetOrderByIdAsync(orderId);
        if (order is null)
        {
            NotifyError("Error.OrderIsNotFound");
            return RedirectToAction(nameof(List));
        }

        var model = await deliveryNoteModelFactory.PrepareCreateDeliveryNoteModelAsync(orderId);

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

    [HttpPost]
    [Authorize(Policy = SystemPermissions.DeliveryNotes.Manage)]
    public async Task<IActionResult> Create(CreateDeliveryNoteModel model)
    {
        if (!ModelState.IsValid)
        {
            model = await deliveryNoteModelFactory.PrepareCreateDeliveryNoteModelAsync(model.OrderId, model);
            return View(model);
        }

        var order = await orderAppService.GetOrderByIdAsync(model.OrderId);
        if (order is null)
        {
            NotifyError("Error.OrderIsNotFound");
            return RedirectToAction(nameof(List));
        }
        if (!order.CanProcess)
        {
            NotifyError("Error.OrderCannotProcess");
            return View(model);
        }

        var deliveryNoteItems = BuildCreateDeliveryNoteItems(model);
        if (!deliveryNoteItems.Any())
        {
            AddLocalizedModelError("Error.DeliveryNoteItemRequired");
            model = await deliveryNoteModelFactory.PrepareCreateDeliveryNoteModelAsync(model.OrderId, model);
            return View(model);
        }


        var orderDiscount = order.OrderDiscount ?? 0;
        var deliveryNotes = await deliveryNoteAppService.GetByOrderIdAsync(order.Id);

        var appliedOrderDiscount = deliveryNotes.Sum(d => d.AppliedOrderDiscount);
        if (appliedOrderDiscount + model.ApplyingOrderDiscount > orderDiscount)
        {
            AddLocalizedModelError("Error.AppliedOrderDiscountExceedOrderDiscount");
            model = await deliveryNoteModelFactory.PrepareCreateDeliveryNoteModelAsync(model.OrderId, model);
            return View(model);
        }

        var appliedOrderPaidAmount = deliveryNotes.Sum(d => d.AppliedOrderPrepaid);
        var orderPaidAmount = await mediator.Send(new GetOrderPaidAmountQuery { OrderId = order.Id });
        if (appliedOrderPaidAmount + model.ApplyingPrepaidAmount > orderPaidAmount)
        {
            AddLocalizedModelError("Error.AppliedOrderPrepaidExceedPrepaidAmount");
            model = await deliveryNoteModelFactory.PrepareCreateDeliveryNoteModelAsync(model.OrderId, model);
            return View(model);
        }
        var amountAlreadyPaidForOrder = Math.Max(0, orderPaidAmount - appliedOrderPaidAmount);

        if (order.CustomerId == cachedValuesService.DefaultCustomerId)
        {
            var productTotal = 0m;
            foreach (var deliveryNoteItem in deliveryNoteItems)
            {
                var orderItem = order.Items.FirstOrDefault(item => item.Id == deliveryNoteItem.OrderItemId);
                if (orderItem is null)
                {
                    AddLocalizedModelError("Error.OrderItemIsNotFound");
                    model = await deliveryNoteModelFactory.PrepareCreateDeliveryNoteModelAsync(model.OrderId, model);
                    return View(model);
                }
                productTotal += deliveryNoteItem.Quantity * orderItem.UnitPrice;
            }
            var calculatedAmountToCollect = model.Surcharge + Math.Max(0, productTotal - model.ApplyingPrepaidAmount - model.ApplyingOrderDiscount);
            if (calculatedAmountToCollect != model.AmountToCollect)
            {
                AddLocalizedModelError("Error.AmountToCollectIsInvalid");
                model = await deliveryNoteModelFactory.PrepareCreateDeliveryNoteModelAsync(model.OrderId, model);
                return View(model);
            }
        }

        try
        {
            var result = await mediator.Send(new CreateDeliveryNoteCommand
            {
                OrderId = model.OrderId,
                ShippingAddress = model.ShippingAddress,
                ShippingPhoneNumber = model.ShippingPhoneNumber,
                ShowPrice = model.ShowPrice,
                Note = model.Note,
                Surcharge = model.Surcharge,
                SurchargeReason = model.SurchargeReason,
                AmountToCollect = model.AmountToCollect,
                Items = deliveryNoteItems,
                AppliedOrderDiscount = model.ApplyingOrderDiscount,
                AppliedOrderPrepaid = model.ApplyingPrepaidAmount
            });

            if (result.Success)
            {
                NotifySuccess(result.SuccessMessage ?? "Msg.SaveSuccess");
                return RedirectToAction(nameof(Details), new { id = result.CreatedId });
            }

            AddLocalizedModelError(result.ErrorMessage ?? "Error.DeliveryNoteCreateFailed");
            model = await deliveryNoteModelFactory.PrepareCreateDeliveryNoteModelAsync(model.OrderId, model);
            return View(model);
        }
        catch (NamEcommerceDomainException ex)
        {
            AddLocalizedModelError(ex.ErrorCode, ex.Parameters);
            model = await deliveryNoteModelFactory.PrepareCreateDeliveryNoteModelAsync(model.OrderId, model);
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

        var warehouses = (await mediator.Send(new GetWarehouseOptionListQuery()))
            .Options
            .ToList();

        var items = new List<object>(distinctProductIds.Count);
        foreach (var productId in distinctProductIds)
        {
            var warehouseItems = new List<object>(warehouses.Count);
            foreach (var warehouse in warehouses)
            {
                var stockInfo = await mediator.Send(new GetProductStockInfoQuery(productId, warehouse.Id));
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
            var model = await deliveryNoteModelFactory.PrepareDeliveryNoteDetailsModelAsync(id);
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
        var result = await mediator.Send(new AssignDeliveryUserCommand
        {
            DeliveryNoteId = id,
            AssignedDeliveryUserId = assignedDeliveryUserId
        });

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
            var model = await deliveryNoteModelFactory.PrepareDeliveryNoteDetailsModelAsync(id);
            var qrCode = await mediator.Send(new CreateDeliveryQrCodeCommand(id)
            {
                CustomerPortalUrl = configuration.GetValue<string>("CustomerPortal:DeliveryNoteUrl") ?? string.Empty
            });
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
    [Authorize(Policy = SystemPermissions.DeliveryNotes.Approve)]
    public async Task<IActionResult> Confirm(Guid id)
    {
        var deliveryNote = await deliveryNoteAppService.GetByIdAsync(id);
        if (deliveryNote is null)
            return this.JsonError(LocalizeError("Error.DeliveryNoteNotFound"));

        var order = await orderAppService.GetOrderByIdAsync(deliveryNote.OrderId);
        if (order is null)
            return this.JsonError(LocalizeError("Error.OrderItemIsNotFound"));
        if (!order.CanProcess)
            return this.JsonError(LocalizeError("Error.OrderCannotProcess"));

        var result = await mediator.Send(new ConfirmDeliveryNoteCommand
        {
            DeliveryNoteId = id
        });

        if (result.Success)
            return this.JsonOk(message: Localizer["Msg.SaveSuccess"]);

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
        var deliveryNote = await deliveryNoteAppService.GetByIdAsync(id);
        if (deliveryNote is null)
            return this.JsonError(LocalizeError("Error.DeliveryNoteNotFound"));

        var result = await mediator.Send(new MarkDeliveringDeliveryNoteCommand
        {
            DeliveryNoteId = id
        });

        if (!result.Success)
            return this.JsonError(LocalizeError(result.ErrorMessage ?? "Error.DeliveryNoteMarkDeliveringFailed"));

        return this.JsonOk(message: Localizer["Msg.SaveSuccess"]);
    }

    [HttpPost]
    [Authorize(Policy = SystemPermissions.DeliveryNotes.Manage)]
    public async Task<IActionResult> MarkDelivered(MarkDeliveryNoteAsDeliveredModel model)
    {
        if (!ModelState.IsValid)
            return this.JsonError(LocalizeError("Error.DeliveryNoteNotFound"));

        var deliveryNote = await deliveryNoteAppService.GetByIdAsync(model.DeliveryNoteId);
        if (deliveryNote is null)
            return this.JsonError(LocalizeError("Error.DeliveryNoteNotFound"));

        var effectivePictureIds = model.PictureIds?.Where(pictureId => pictureId != Guid.Empty).ToList() ?? [];
        if (effectivePictureIds.Count == 0
            && deliveryNote.Status == (int)DeliveryNoteStatus.PendingConfirmation
            && deliveryNote.DeliveryProofPictureId.HasValue)
        {
            effectivePictureIds.Add(deliveryNote.DeliveryProofPictureId.Value);
        }
        if (effectivePictureIds.Count == 0)
            return this.JsonError(LocalizeError("Error.DeliveryProofRequired"));

        var acceptanceItems = ParseAcceptanceItems(model.AcceptanceItemsJson);
        var result = await mediator.Send(new MarkDeliveryNoteDeliveredCommand
        {
            DeliveryNoteId = model.DeliveryNoteId,
            ReceiverName = model.ReceiverName,
            AgreedCustomerCharge = model.AgreedCustomerCharge,
            AgreedCustomerChargeReason = model.AgreedCustomerChargeReason,
            CompensateInNextDelivery = model.CompensateInNextDelivery,
            CashCollectedAmount = model.CashCollectedAmount,
            Items = acceptanceItems,
            PictureIds = effectivePictureIds
        });
        if (result.Success)
            return this.JsonOk(Localizer["Msg.SaveSuccess"]);

        return this.JsonError(LocalizeError(result.ErrorMessage!));

        // local method
        static IList<MarkDeliveryNoteDeliveredItemCommand> ParseAcceptanceItems(string? acceptanceItemsJson)
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
    }

    public async Task<IActionResult> GetDeliveryNoteAcceptantItemsInfo(Guid id)
    {
        var deliveryNote = await deliveryNoteAppService.GetByIdAsync(id);
        if (deliveryNote is null)
            return this.JsonError(LocalizeError("Error.DeliveryNoteNotFound"));

        var returnedQuantities = await mediator.Send(new GetReturnedQuantitiesByDeliveryNoteQuery
        {
            DeliveryNoteId = id
        });

        var productIds = deliveryNote.Items.Select(i => i.ProductId).Distinct().ToList();
        var products = await mediator.Send(new GetProductsByIdsForOrderQuery { Ids = productIds });
        var decimalPlacesByProductId = products.ToDictionary(p => p.Id, p => p.QuantityDecimalPlaces);

        Dictionary<Guid, DeliveryNoteSettlementItemAppDto> settlementItemsByDeliveryNoteItemId = deliveryNote.Status == (int)DeliveryNoteStatus.PendingConfirmation
            ? deliveryNote.SettlementItems.ToDictionary(item => item.DeliveryNoteItemId)
            : [];

        var acceptantItems = deliveryNote.Items.Select(i =>
        {
            returnedQuantities.TryGetValue(i.Id, out var summary);
            settlementItemsByDeliveryNoteItemId.TryGetValue(i.Id, out var settlementItem);
            return new DeliveryNoteDetailsModel.DeliveryNoteItemModel
            {
                Id = i.Id,
                ProductName = i.ProductName,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                SubTotal = i.SubTotal,
                ReturnedQuantity = settlementItem?.RejectedQuantity ?? summary?.ConfirmedQuantity ?? 0m,
                RejectReason = settlementItem?.RejectReason,
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

        var currentUser = await currentUserAccessor.GetCurrentUserAsync();
        var result = await mediator.Send(new ApproveDeliverySettlementCommand
        {
            DeliveryNoteId = deliveryNoteId,
            ApprovedAmountToCollect = approvedAmountToCollect,
            AgreedCustomerCharge = agreedCustomerCharge,
            AgreedCustomerChargeReason = agreedCustomerChargeReason,
            AdminNote = adminNote,
            ApprovedByUserId = currentUser?.Id
        });

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

        var currentUser = await currentUserAccessor.GetCurrentUserAsync();
        var result = await mediator.Send(new RejectDeliverySettlementCommand
        {
            DeliveryNoteId = deliveryNoteId,
            Reason = reason,
            ApprovedByUserId = currentUser?.Id
        });

        if (result.Success)
            NotifySuccess(result.SuccessMessage ?? "Msg.SaveSuccess");
        else
            NotifyError(result.ErrorMessage ?? "Error.GenericError");

        return RedirectToAction(nameof(Details), new { id = deliveryNoteId });
    }

    [HttpPost]
    [Authorize(Policy = SystemPermissions.DeliveryNotes.Manage)]
    public async Task<IActionResult> AdminUpdateAmountToCollect(Guid deliveryNoteId, decimal newAmount, string? note)
    {
        if (!User.IsInRole(SystemUserRoleNames.Admin))
            return Forbid();

        var currentUser = await currentUserAccessor.GetCurrentUserAsync();
        var result = await mediator.Send(new AdminUpdateAmountToCollectCommand
        {
            DeliveryNoteId = deliveryNoteId,
            NewAmount = newAmount,
            Note = note,
            AdminUserId = currentUser?.Id
        });

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
        var deliveryNote = await deliveryNoteAppService.GetByIdAsync(id);
        if (deliveryNote is null)
            return this.JsonError(LocalizeError("Error.DeliveryNoteNotFound"));

        var order = await orderAppService.GetOrderByIdAsync(deliveryNote.OrderId);
        if (order is null)
            return this.JsonError(LocalizeError("Error.OrderItemIsNotFound"));
        if (!order.CanProcess)
            return this.JsonError(LocalizeError("Error.OrderCannotProcess"));

        await mediator.Send(new CancelDeliveryNoteCommand
        {
            DeliveryNoteId = id
        });

        return this.JsonOk(message: LocalizeError("Msg.SaveSuccess"));
    }

    [HttpPost]
    [Authorize(Policy = SystemPermissions.DeliveryNotes.Manage)]
    public async Task<IActionResult> CreateFromPreparation([FromBody] CreateFromPreparationRequest request)
    {
        if (request == null || !request.SelectedItems.Any())
        {
            return Json(new { success = false, message = LocalizeError("Error.DeliveryNoteItemRequired") });
        }

        var model = await deliveryNoteModelFactory.PrepareCreateDeliveryNoteModelAsync(request.OrderId);

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

        var createResult = await mediator.Send(new CreateDeliveryNoteCommand
        {
            OrderId = model.OrderId,
            Note = model.Note,
            ShowPrice = model.ShowPrice,
            ShippingAddress = model.ShippingAddress,
            ShippingPhoneNumber = model.ShippingPhoneNumber,
            Surcharge = request.Surcharge,
            SurchargeReason = request.SurchargeReason,
            AmountToCollect = request.AmountToCollect,
            Items = request.SelectedItems.Select(selectedItem => new CreateDeliveryNoteCommand.CreateDeliveryNoteItemModel
            {
                OrderItemId = selectedItem.OrderItemId,
                WarehouseId = selectedItem.WarehouseId,
                Quantity = selectedItem.Quantity
            }).ToList()
        });

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

            var warehouseId = item.WarehouseId;
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
        var result = await mediator.Send(new UpdateDeliveryNoteShippingCommand(
            deliveryNoteId,
            shippingAddress,
            shippingPhoneNumber));

        if (result.Success)
            NotifySuccess("Msg.SaveSuccess");
        else
            NotifyError(result.ErrorMessage ?? "Error.DeliveryNoteCannotUpdateShipping");

        return RedirectToAction(nameof(Details), new { id = deliveryNoteId });
    }

    #region Helper classes

    public class CreateFromPreparationRequest
    {
        public Guid OrderId { get; set; }
        public List<SelectedItemModel> SelectedItems { get; set; } = [];
        public bool ShowPrice { get; set; }
        public bool CompensateReturnedQuantityInNextDelivery { get; set; }
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
}
