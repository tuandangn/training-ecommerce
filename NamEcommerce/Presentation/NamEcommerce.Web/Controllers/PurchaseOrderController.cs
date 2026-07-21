using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NamEcommerce.Application.Contracts.PurchaseOrders;
using NamEcommerce.Web.Contracts.Commands.Models.PurchaseOrders;
using NamEcommerce.Web.Services.PurchaseOrders;
using NamEcommerce.Web.Models.PurchaseOrders;
using NamEcommerce.Web.Contracts.Queries.Models.PurchaseOrders;
using NamEcommerce.Web.Contracts.Queries.Models.Catalog;
using NamEcommerce.Application.Contracts.Orders;
using NamEcommerce.Web.Contracts.Security;
using NamEcommerce.Web.Extensions;
using NamEcommerce.Domain.Shared.Enums.PurchaseOrders;
using NamEcommerce.Application.Contracts.Security;
using NamEcommerce.Application.Contracts.Users;

namespace NamEcommerce.Web.Controllers;

public sealed class PurchaseOrderController : BaseAuthorizedController
{
    private readonly IMediator _mediator;
    private readonly IPurchaseOrderModelFactory _purchaseOrderModelFactory;
    private readonly IPurchaseOrderAppService _purchaseOrderAppService;
    private readonly IOrderAppService _orderAppService;
    private readonly IAuthorizationAppService _authorizationAppService;
    private readonly ICurrentUserService _currentUserService;

    public PurchaseOrderController(IMediator mediator,
        IPurchaseOrderModelFactory purchaseOrderModelFactory, IPurchaseOrderAppService purchaseOrderAppService,
        IOrderAppService orderAppService, IAuthorizationAppService authorizationAppService, ICurrentUserService currentUserService)
    {
        _mediator = mediator;
        _purchaseOrderModelFactory = purchaseOrderModelFactory;
        _purchaseOrderAppService = purchaseOrderAppService;
        _orderAppService = orderAppService;
        _authorizationAppService = authorizationAppService;
        _currentUserService = currentUserService;
    }

    public IActionResult Index() => RedirectToAction(nameof(List));

    [Authorize(Policy = SystemPermissions.PurchaseOrders.View)]
    public async Task<IActionResult> List(PurchaseOrderListSearchModel searchModel)
    {
        var model = await _purchaseOrderModelFactory.PreparePurchaseOrderListModel(searchModel);
        return View(model);
    }

    [Authorize(Policy = SystemPermissions.PurchaseOrders.Create)]
    public async Task<IActionResult> Create()
    {
        var model = await _purchaseOrderModelFactory.PrepareCreatePurchaseOrderModel();
        return View(model);
    }

    [Authorize(Policy = SystemPermissions.PurchaseOrders.View)]
    public async Task<IActionResult> ShortageAggregation([FromQuery] GetShortageAggregationQuery query)
    {
        var model = await _mediator.Send(query).ConfigureAwait(false);
        return View(model);
    }

    public async Task<IActionResult> CheckExistingDrafts([FromBody] CheckExistingDraftsCommand command)
    {
        var result = await _mediator.Send(command).ConfigureAwait(false);
        return Json(result);
    }

    public async Task<IActionResult> CheckRelatedPurchaseOrders([FromBody] CheckRelatedPurchaseOrdersCommand command)
    {
        var result = await _mediator.Send(command).ConfigureAwait(false);
        return Json(result);
    }

    [HttpPost]
    [Authorize(Policy = SystemPermissions.PurchaseOrders.Create)]
    public async Task<IActionResult> CreateFromShortage([FromBody] CreatePurchaseOrdersFromShortageCommand command)
    {
        var result = await _mediator.Send(command).ConfigureAwait(false);
        if (!result.Success)
            return this.JsonError(LocalizeError(result.ErrorMessage!));

        return this.JsonOk(result.Items, Localizer["Msg.PurchaseOrders.CreateSuccess", result.Items.Count].Value);
    }

    [HttpPost]
    [Authorize(Policy = SystemPermissions.PurchaseOrders.Create)]
    public async Task<IActionResult> Create(CreatePurchaseOrderModel model)
    {
        if (!ModelState.IsValid)
        {
            model = await _purchaseOrderModelFactory.PrepareCreatePurchaseOrderModel(model);
            return View(model);
        }

        var productInfos = model.Items.Count == 0 ? []
            : await _mediator.Send(new GetProductsByIdsForOrderQuery
            {
                Ids = model.Items.Where(i => i.ProductId.HasValue).Select(i => i.ProductId!.Value).ToList()
            });

        if (model.Items.Count > 0)
        {
            if (model.Items.Any(item => !productInfos.Any(p => p.Id == item.ProductId)))
            {
                AddLocalizedModelError("Error.PurchaseOrder.ProductIsNotFound");
                model = await _purchaseOrderModelFactory.PrepareCreatePurchaseOrderModel(model);
                return View(model);
            }

            if (model.VendorId.HasValue)
            {
                var isInvalid = false;

                var candidateVendorIds = productInfos.SelectMany(p => p.AvailableVendors).Select(v => v.Id).Distinct().ToList();
                var validVendorIds = candidateVendorIds.Where(vendorId => productInfos.All(p => p.AvailableVendors.Any(v => v.Id == vendorId))).ToList();

                model.NotHasAppropriatedVendor = isInvalid = validVendorIds.Count == 0;

                if (model.VendorId.HasValue && !validVendorIds.Contains(model.VendorId.Value))
                {
                    AddLocalizedModelError("Error.PurchaseOrder.VendorIsNotAppropriate");
                    model.VendorId = null;
                    isInvalid = true;
                }

                if (isInvalid)
                {
                    model = await _purchaseOrderModelFactory.PrepareCreatePurchaseOrderModel(model);
                    return View(model);
                }
            }
        }

        var result = await _mediator.Send(new CreatePurchaseOrderCommand
        {
            PlacedOn = model.PlacedOn,
            VendorId = model.VendorId!.Value,
            WarehouseId = model.WarehouseId,
            Note = model.Note,
            ExpectedDeliveryDate = model.ExpectedDeliveryDate,
            Items = model.Items.Select(i => new CreatePurchaseOrderItemCommand
            {
                ProductId = i.ProductId,
                Quantity = i.Quantity ?? 0,
                UnitCost = i.UnitCost ?? 0
            }).ToList()
        });

        if (!result.Success)
        {
            AddLocalizedModelError(result.ErrorMessage);
            model = await _purchaseOrderModelFactory.PrepareCreatePurchaseOrderModel(model);
            return View(model);
        }

        NotifySuccess("Msg.SaveSuccess");
        return RedirectToAction(nameof(Details), new { id = result.CreatedId });
    }

    [Authorize(Policy = SystemPermissions.PurchaseOrders.QuickCreate)]
    public async Task<IActionResult> QuickCreate()
    {
        var model = await _purchaseOrderModelFactory.PrepareQuickCreatePurchaseOrderModel();
        return View(model);
    }

    [HttpPost]
    [Authorize(Policy = SystemPermissions.PurchaseOrders.QuickCreate)]
    public async Task<IActionResult> QuickCreate([FromBody] QuickCreatePurchaseOrderCommand command)
    {
        var result = await _mediator.Send(command).ConfigureAwait(false);
        if (!result.Success)
            return this.JsonError(LocalizeError(result.ErrorMessage!));

        return this.JsonOk(new { purchaseOrderId = result.PurchaseOrderId }, Localizer["Msg.SaveSuccess"].Value);
    }

    [HttpPost]
    [Authorize(Policy = SystemPermissions.PurchaseOrders.Edit)]
    public async Task<IActionResult> Edit(EditPurchaseOrderModel model)
    {
        if (!ModelState.IsValid)
        {
            NotifyError("Error.InvalidRequest", GetErrorMessage());
            return RedirectToAction(nameof(Details), new { id = model.Id });
        }

        var updatePurchaseOrderResult = await _mediator.Send(new UpdatePurchaseOrderCommand
        {
            PurchaseOrderId = model.Id,
            PlacedOn = model.PlacedOn,
            ShippingAmount = model.ShippingAmount ?? 0,
            TaxAmount = model.TaxAmount ?? 0,
            VendorId = model.VendorId,
            WarehouseId = model.WarehouseId,
            ExpectedDeliveryDate = model.ExpectedDeliveryDate,
            Note = model.Note
        });

        if (!updatePurchaseOrderResult.Success)
        {
            NotifyError(updatePurchaseOrderResult.ErrorMessage!);
            return RedirectToAction(nameof(Details), new { id = model.Id });
        }
        NotifySuccess("Msg.SaveSuccess");
        return RedirectToAction(nameof(Details), new { id = model.Id });
    }

    [Authorize(Policy = SystemPermissions.PurchaseOrders.View)]
    public async Task<IActionResult> Details(Guid id)
    {
        var model = await _purchaseOrderModelFactory.PreparePurchaseOrderDetailsModel(id);
        if (model == null)
        {
            NotifyError("Error.PurchaseOrderIsNotFound");
            return RedirectToAction(nameof(List));
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = SystemPermissions.Debts.VendorDebtsRecordPayment)]
    public async Task<IActionResult> RecordSettlementPayment(RecordPurchaseOrderSettlementPaymentCommand command)
    {
        var result = await _mediator.Send(command).ConfigureAwait(false);
        if (result.Success)
            NotifySuccess("Msg.SaveSuccess");
        else
            NotifyError(result.ErrorMessage ?? "Error.InvalidRequest");

        return RedirectToAction(nameof(Details), new { id = command.PurchaseOrderId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = SystemPermissions.PurchaseOrders.Create)]
    public async Task<IActionResult> Copy(Guid id)
    {
        var result = await _mediator.Send(new CopyPurchaseOrderCommand(id)).ConfigureAwait(false);
        if (result.Success && result.CreatedId.HasValue)
        {
            NotifySuccess("Msg.SaveSuccess");
            return RedirectToAction(nameof(Details), new { id = result.CreatedId.Value });
        }

        NotifyError(result.ErrorMessage ?? "Error.InvalidRequest");
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [Authorize(Policy = SystemPermissions.PurchaseOrders.Create)]
    public async Task<IActionResult> Split([FromBody] SplitPurchaseOrderCommand command)
    {
        var result = await _mediator.Send(command).ConfigureAwait(false);
        if (result.Success && result.CreatedId.HasValue)
            return this.JsonOk(new { newPurchaseOrderId = result.CreatedId.Value });

        return this.JsonError(LocalizeError(result.ErrorMessage ?? "Error.InvalidRequest"));
    }

    [HttpPost]
    [Authorize(Policy = SystemPermissions.PurchaseOrders.Create)]
    public async Task<IActionResult> AddPurchaseOrderItem(AddPurchaseOrderItemModel model)
    {
        if (!ModelState.IsValid)
            return Json(new { success = false, message = GetErrorMessage() });

        var purchaseOrder = await _mediator.Send(new GetPurchaseOrderQuery { Id = model.PurchaseOrderId });
        if (purchaseOrder is null)
            return Json(new { success = false, message = LocalizeError("Error.PurchaseOrderIsNotFound") });

        var result = await _mediator.Send(new AddPurchaseOrderItemCommand
        {
            PurchaseOrderId = model.PurchaseOrderId,
            ProductId = model.ProductId ?? default,
            Quantity = model.Quantity ?? 0,
            UnitCost = model.UnitCost ?? 0,
            Note = model.Note,
            QuantityDecimalPlaces = model.QuantityDecimalPlaces
        });

        if (!result.Success)
            return Json(new { success = false, message = LocalizeError(result.ErrorMessage!) });
        return Json(new { success = true, message = string.Empty });
    }

    [HttpPost]
    [Authorize(Policy = SystemPermissions.PurchaseOrders.Edit)]
    public async Task<IActionResult> Receive(ReceivePurchaseOrderItemModel model)
    {
        if (!ModelState.IsValid)
        {
            NotifyError("Error.InvalidRequest", GetErrorMessage());
            return RedirectToAction(nameof(Details), new { id = model.PurchaseOrderId });
        }

        var purchaseOrder = await _mediator.Send(new GetPurchaseOrderQuery { Id = model.PurchaseOrderId });
        if (purchaseOrder is null)
        {
            NotifyError("Error.PurchaseOrderIsNotFound");
            return RedirectToAction(nameof(List));
        }

        if (model.DirectShipOrderId.HasValue)
        {
            var order = await _orderAppService.GetOrderByIdAsync(model.DirectShipOrderId.Value);
            if (order == null)
            {
                NotifyError("Error.OrderIsNotFound");
                return RedirectToAction(nameof(List));
            }
            var orderItem = order.Items.Where(item => item.Id == model.DirectShipOrderItemId).FirstOrDefault();
            if (order == null)
            {
                NotifyError("Error.OrderItemIsNotFound");
                return RedirectToAction(nameof(List));
            }
        }

        var result = await _mediator.Send(new ReceivePurchaseOrderItemCommand
        {
            PurchaseOrderId = model.PurchaseOrderId,
            PurchaseOrderItemId = model.PurchaseOrderItemId,
            ReceivedQuantity = model.ReceivedQuantity,
            WarehouseId = model.WarehouseId,
            SellingPrice = model.SellingPrice,
            OversupplyAction = model.OversupplyAction,
            DirectShipOrderId = model.DirectShipOrderId,
            DirectShipOrderItemId = model.DirectShipOrderItemId,
            DirectShipAddress = model.DirectShipAddress,
            DirectShipContactName = model.DirectShipContactName,
            DirectShipContactPhone = model.DirectShipContactPhone,
            DirectShipExistingAllocationId = model.DirectShipExistingAllocationId
        });

        if (!result.Success)
        {
            NotifyError(result.ErrorMessage!);
            return RedirectToAction(nameof(Details), new { id = model.PurchaseOrderId });
        }

        NotifySuccess("Msg.SaveSuccess");
        return RedirectToAction(nameof(Details), new { id = model.PurchaseOrderId });
    }

    [HttpPost]
    [Authorize(Policy = SystemPermissions.PurchaseOrders.Create)]
    public async Task<IActionResult> UpdatePurchaseOrderItem([FromBody] EditPurchaseOrderItemModel model)
    {
        if (!ModelState.IsValid)
            return Json(new { success = false, errorMessage = LocalizeError("Error.InvalidRequest", GetErrorMessage()) });

        var purchaseOrder = await _mediator.Send(new GetPurchaseOrderQuery { Id = model.PurchaseOrderId });
        if (purchaseOrder is null)
            return Json(new { success = false, errorMessage = LocalizeError("Error.PurchaseOrderIsNotFound") });

        var purchaseOrderItem = purchaseOrder.Items.FirstOrDefault(item => item.Id == model.PurchaseOrderItemId);
        if (purchaseOrderItem is null)
            return Json(new { success = false, errorMessage = LocalizeError("Error.PurchaseOrderItemIsNotFound") });

        if (!purchaseOrder.CanAddItems)
            return Json(new { success = false, errorMessage = LocalizeError("Error.PurchaseOrderCannotUpdateOrderItems") });

        var result = await _mediator.Send(new UpdatePurchaseOrderItemCommand
        {
            PurchaseOrderId = model.PurchaseOrderId,
            PurchaseOrderItemId = model.PurchaseOrderItemId,
            Quantity = model.Quantity ?? 0,
            UnitCost = model.UnitCost ?? 0,
            Note = model.Note
        });

        if (!result.Success)
            return Json(new { success = false, errorMessage = LocalizeError(result.ErrorMessage!) });

        return Json(new { success = true });
    }

    [HttpPost]
    [Authorize(Policy = SystemPermissions.PurchaseOrders.Edit)]
    public async Task<IActionResult> BulkReceive(BulkReceivePurchaseOrderModel model)
    {
        if (!ModelState.IsValid)
        {
            NotifyError("Error.InvalidRequest", GetErrorMessage());
            return RedirectToAction(nameof(Details), new { id = model.PurchaseOrderId });
        }

        var purchaseOrder = await _mediator.Send(new GetPurchaseOrderQuery { Id = model.PurchaseOrderId });
        if (purchaseOrder is null)
        {
            NotifyError("Error.PurchaseOrderIsNotFound");
            return RedirectToAction(nameof(List));
        }

        var lines = (model.Items ?? [])
            .Where(line => line.ItemId != Guid.Empty && line.Quantity > 0)
            .Select(line => new BulkReceiveLineCommand
            {
                ItemId = line.ItemId,
                Quantity = line.Quantity,
                WarehouseId = line.WarehouseId,
                ActualUnitCost = line.ActualUnitCost,
                DirectShipOrderId = line.DirectShipOrderId,
                DirectShipOrderItemId = line.DirectShipOrderItemId,
                DirectShipAddress = line.DirectShipAddress,
                DirectShipContactName = line.DirectShipContactName,
                DirectShipContactPhone = line.DirectShipContactPhone,
                DirectShipExistingAllocationId = line.DirectShipExistingAllocationId
            })
            .ToList();

        if (lines.Count == 0)
        {
            NotifyError("Error.BulkReceive.NoItemsProvided");
            return RedirectToAction(nameof(Details), new { id = model.PurchaseOrderId });
        }

        var result = await _mediator.Send(new BulkReceivePurchaseOrderCommand
        {
            PurchaseOrderId = model.PurchaseOrderId,
            Items = lines,
            AdditionalShipping = model.AdditionalShipping ?? 0,
            AdditionalTax = model.AdditionalTax ?? 0
        });

        if (!result.Success)
            NotifyError(result.ErrorMessage!);
        else
        {
            var count = result.CreatedGoodsReceiptIds.Count;
            if (count > 1)
                NotifySuccess("Msg.BulkReceive.CreatedMultiple", count);
            else
                NotifySuccess("Msg.SaveSuccess");
        }

        return RedirectToAction(nameof(Details), new { id = model.PurchaseOrderId });
    }

    [HttpPost]
    [Authorize(Policy = SystemPermissions.PurchaseOrders.Create)]
    public async Task<IActionResult> RemovePurchaseOrderItem([FromBody] DeletePurchaseOrderItemModel model)
    {
        if (!ModelState.IsValid)
            return Json(new { success = false, errorMessage = LocalizeError("Error.InvalidRequest", GetErrorMessage()) });

        var purchaseOrder = await _mediator.Send(new GetPurchaseOrderQuery { Id = model.PurchaseOrderId });
        if (purchaseOrder is null)
            return Json(new { success = false, errorMessage = LocalizeError("Error.PurchaseOrderIsNotFound") });

        var purchaseOrderItem = purchaseOrder.Items.FirstOrDefault(item => item.Id == model.PurchaseOrderItemId);
        if (purchaseOrderItem is null)
            return Json(new { success = false, errorMessage = LocalizeError("Error.PurchaseOrderItemIsNotFound") });

        if (!purchaseOrder.CanAddItems)
            return Json(new { success = false, errorMessage = LocalizeError("Error.PurchaseOrderCannotUpdateOrderItems") });

        var result = await _mediator.Send(new DeletePurchaseOrderItemCommand
        {
            PurchaseOrderId = model.PurchaseOrderId,
            ItemId = model.PurchaseOrderItemId
        });

        if (!result.Success)
            return Json(new { success = false, errorMessage = LocalizeError(result.ErrorMessage!) });
        return Json(new { success = true });
    }

    [HttpPost]
    [Authorize(Policy = SystemPermissions.PurchaseOrders.Create)]
    public async Task<IActionResult> SubmitsPurchaseOrder(Guid id)
    {
        var purchaseOrder = await _mediator.Send(new GetPurchaseOrderQuery { Id = id });
        if (purchaseOrder is null)
        {
            NotifyError("Error.PurchaseOrderIsNotFound");
            return RedirectToAction(nameof(List));
        }

        var (success, errorMessage) = await _mediator.Send(new SubmitsPurchaseOrderCommand
        {
            PurchaseOrderId = id
        });

        if (success)
            NotificationService.Success(errorMessage ?? LocalizeError("Msg.SaveSuccess"));
        else
            NotifyError(errorMessage!);
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [Authorize(Policy = SystemPermissions.PurchaseOrders.Approve)]
    public async Task<IActionResult> ApprovesPurchaseOrder(Guid id)
    {
        var purchaseOrder = await _mediator.Send(new GetPurchaseOrderQuery { Id = id });
        if (purchaseOrder is null)
        {
            NotifyError("Error.PurchaseOrderIsNotFound");
            return RedirectToAction(nameof(List));
        }

        var (success, errorMessage) = await _mediator.Send(new ApprovesPurchaseOrderCommand
        {
            PurchaseOrderId = id
        });

        if (success)
            NotificationService.Success(errorMessage ?? LocalizeError("Msg.SaveSuccess"));
        else
            NotifyError(errorMessage!);
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [Authorize(Policy = SystemPermissions.PurchaseOrders.Cancel)]
    public async Task<IActionResult> CancelPurchaseOrder(Guid id)
    {
        var purchaseOrder = await _mediator.Send(new GetPurchaseOrderQuery { Id = id });
        if (purchaseOrder is null)
        {
            NotifyError("Error.PurchaseOrderIsNotFound");
            return RedirectToAction(nameof(List));
        }

        var (success, errorMessage) = await _mediator.Send(new CancelPurchaseOrderCommand
        {
            PurchaseOrderId = id
        });

        if (success)
            NotificationService.Success(errorMessage ?? LocalizeError("Msg.SaveSuccess"));
        else
            NotifyError(errorMessage!);
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [Authorize(Policy = SystemPermissions.PurchaseOrders.Edit)]
    public async Task<IActionResult> ClosePartial(Guid id, string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            NotifyError("Error.PurchaseOrder.CloseReasonRequired");
            return RedirectToAction(nameof(Details), new { id });
        }

        var (success, errorMessage) = await _mediator.Send(new ClosePartialPurchaseOrderCommand
        {
            PurchaseOrderId = id,
            Reason = reason
        });

        if (success)
            NotifySuccess("Msg.SaveSuccess");
        else
            NotifyError(errorMessage!);
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpGet]
    [Authorize(Policy = SystemPermissions.PurchaseOrders.View)]
    public async Task<IActionResult> RecentPurchasePrices(Guid productId)
    {
        if (productId == Guid.Empty)
            return Json(Array.Empty<object>());

        var prices = await _mediator.Send(new GetRecentPurchasePricesQuery { ProductId = productId });

        var result = prices.Select(p => new
        {
            vendorId = p.VendorId,
            vendorName = p.VendorName,
            unitCost = p.UnitCost,
            purchaseOrderCode = p.PurchaseOrderCode,
            purchaseDate = p.PurchaseDate.ToString("dd/MM/yyyy")
        });

        return Json(result);
    }

    [HttpPost]
    [Authorize(Policy = SystemPermissions.PurchaseOrders.Edit)]
    public async Task<IActionResult> ChangeStatus(Guid id, int status)
    {
        if ((PurchaseOrderStatus)status == PurchaseOrderStatus.Approved)
        {
            NotifyError("Error.PurchaseOrder.ApproveRequiresPermission");
            return RedirectToAction(nameof(Details), new { id });
        }

        var purchaseOrder = await _mediator.Send(new GetPurchaseOrderQuery { Id = id });
        if (purchaseOrder is null)
        {
            NotifyError("Error.PurchaseOrderIsNotFound");
            return RedirectToAction(nameof(List));
        }

        var result = await _mediator.Send(new ChangePurchaseOrderStatusCommand
        {
            PurchaseOrderId = id,
            Status = status
        });

        if (!result.Success)
            NotifyError(result.ErrorMessage!);
        else
            NotifySuccess("Msg.SaveSuccess");

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpGet]
    [Authorize(Policy = SystemPermissions.PurchaseOrders.View)]
    public async Task<IActionResult> EligibleOrderItems(Guid purchaseOrderId, Guid purchaseOrderItemId)
    {
        if (purchaseOrderItemId == Guid.Empty)
            return Json(Array.Empty<object>());

        var items = await _purchaseOrderAppService.GetEligibleOrderItemsForPoItemAsync((purchaseOrderId, purchaseOrderItemId)).ConfigureAwait(false);
        var productIds = items.Select(i => i.ProductId).Distinct();
        var products = await _mediator.Send(new GetProductsByIdsForOrderQuery { Ids = productIds }).ConfigureAwait(false);
        var decimalPlacesByProductId = products.ToDictionary(p => p.Id, p => p.QuantityDecimalPlaces);

        return Json(items.Select(item => new
        {
            orderItemId = item.OrderItemId,
            orderId = item.OrderId,
            orderCode = item.OrderCode,
            customerName = item.CustomerName,
            customerPhone = item.CustomerPhone,
            shippingAddress = item.ShippingAddress,
            productName = item.ProductName,
            quantityDecimalPlaces = decimalPlacesByProductId.GetValueOrDefault(item.ProductId),
            totalQuantity = item.TotalQuantity,
            allocatedOutstanding = item.AllocatedOutstanding,
            availableToAllocate = item.AvailableToAllocate
        }));
    }

    [HttpGet]
    [Authorize(Policy = SystemPermissions.PurchaseOrders.View)]
    public async Task<IActionResult> NonDirectShipAllocations(Guid purchaseOrderId, Guid purchaseOrderItemId)
    {
        if (purchaseOrderItemId == Guid.Empty)
            return Json(Array.Empty<object>());

        var allocations = await _purchaseOrderAppService.GetNonDirectShipAllocationsForPoItemAsync((purchaseOrderId, purchaseOrderItemId)).ConfigureAwait(false);

        return Json(allocations.Select(a => new
        {
            allocationId = a.AllocationId,
            orderId = a.OrderId,
            orderItemId = a.OrderItemId,
            orderCode = a.OrderCode,
            customerName = a.CustomerName,
            customerPhone = a.CustomerPhone,
            shippingAddress = a.ShippingAddress,
            allocatedQty = a.AllocatedQuantity,
            remainingQty = a.RemainingQuantity
        }));
    }

    [HttpPost]
    [Authorize(Policy = SystemPermissions.PurchaseOrders.Edit)]
    public async Task<IActionResult> AllocateToOrder([FromBody] AllocateToOrderRequestModel request)
    {
        if (request is null || request.PurchaseOrderItemId == Guid.Empty || request.OrderItemId == Guid.Empty || request.Quantity <= 0)
            return Json(new { success = false, message = LocalizeError("Error.InvalidRequest") });

        var result = await _mediator.Send(new AllocatePoItemForOrderItemCommand
        {
            PurchaseOrderId = request.PurchaseOrderId,
            PurchaseOrderItemId = request.PurchaseOrderItemId,
            OrderId = request.OrderId,
            OrderItemId = request.OrderItemId,
            Quantity = request.Quantity,
            DirectShipAddress = request.DirectShipAddress,
            DirectShipContactName = request.DirectShipContactName,
            DirectShipContactPhone = request.DirectShipContactPhone
        }).ConfigureAwait(false);

        if (!result.Success)
            return Json(new { success = false, message = LocalizeError(result.ErrorMessage!) });

        return Json(new { success = true });
    }

    [HttpPost]
    [Authorize(Policy = SystemPermissions.PurchaseOrders.Edit)]
    public async Task<IActionResult> ReleaseAllocationsForPurchaseOrderItem([FromBody] ReleaseAllocationsOfPurchaseOrderItemCommand command)
    {
        var result = await _mediator.Send(command).ConfigureAwait(false);
        if (!result.Success)
            return Json(new { success = false, message = LocalizeError(result.ErrorMessage!) });

        return Json(new { success = true });
    }

    [HttpPost]
    [Authorize(Policy = SystemPermissions.PurchaseOrders.Edit)]
    public async Task<IActionResult> MarkAllocationAsDirectShip([FromBody] MarkAllocationAsDirectShipCommand command)
    {
        if (command is null
            || command.AllocationId == Guid.Empty
            || string.IsNullOrWhiteSpace(command.Address)
            || string.IsNullOrWhiteSpace(command.ContactPhone))
        {
            return Json(new { success = false, message = LocalizeError("Error.InvalidRequest") });
        }

        var result = await _mediator.Send(command).ConfigureAwait(false);
        if (!result.Success)
            return Json(new { success = false, message = LocalizeError(result.ErrorMessage!) });

        return Json(new { success = true, message = LocalizeError("Msg.SaveSuccess") });
    }
}
