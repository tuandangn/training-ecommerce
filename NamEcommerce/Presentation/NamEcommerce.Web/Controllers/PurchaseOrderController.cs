using MediatR;
using Microsoft.AspNetCore.Mvc;
using NamEcommerce.Application.Contracts.Dtos.PurchaseOrders;
using NamEcommerce.Application.Contracts.PurchaseOrders;
using NamEcommerce.Web.Contracts.Commands.Models.PurchaseOrders;
using NamEcommerce.Web.Services.PurchaseOrders;
using NamEcommerce.Web.Models.PurchaseOrders;
using NamEcommerce.Web.Contracts.Queries.Models.PurchaseOrders;
using NamEcommerce.Web.Contracts.Queries.Models.Catalog;
using NamEcommerce.Domain.Shared.Enums.PurchaseOrders;

namespace NamEcommerce.Web.Controllers;

public sealed class PurchaseOrderController : BaseAuthorizedController
{
    private readonly IMediator _mediator;
    private readonly IPurchaseOrderModelFactory _purchaseOrderModelFactory;
    private readonly IPurchaseOrderAppService _purchaseOrderAppService;

    public PurchaseOrderController(IMediator mediator, IPurchaseOrderModelFactory purchaseOrderModelFactory, IPurchaseOrderAppService purchaseOrderAppService)
    {
        _mediator = mediator;
        _purchaseOrderModelFactory = purchaseOrderModelFactory;
        _purchaseOrderAppService = purchaseOrderAppService;
    }

    public IActionResult Index() => RedirectToAction(nameof(List));

    public async Task<IActionResult> List(PurchaseOrderListSearchModel searchModel)
    {
        var model = await _purchaseOrderModelFactory.PreparePurchaseOrderListModel(searchModel);
        return View(model);
    }

    public async Task<IActionResult> Create()
    {
        var model = await _purchaseOrderModelFactory.PrepareCreatePurchaseOrderModel();
        return View(model);
    }

    [HttpGet("/PurchaseOrders/ShortageAggregation")]
    public async Task<IActionResult> ShortageAggregation([FromQuery] GetShortageAggregationQuery query)
    {
        var model = await _mediator.Send(query).ConfigureAwait(false);
        return View(model);
    }

    [HttpPost("/PurchaseOrders/CheckExistingDrafts")]
    public async Task<IActionResult> CheckExistingDrafts([FromBody] CheckExistingDraftsCommand command)
    {
        var result = await _mediator.Send(command).ConfigureAwait(false);
        return Json(result);
    }

    [HttpPost("/PurchaseOrders/CheckRelatedPurchaseOrders")]
    public async Task<IActionResult> CheckRelatedPurchaseOrders([FromBody] CheckRelatedPurchaseOrdersCommand command)
    {
        var result = await _mediator.Send(command).ConfigureAwait(false);
        return Json(result);
    }

    [HttpPost("/PurchaseOrders/CreateFromShortage")]
    public async Task<IActionResult> CreateFromShortage([FromBody] CreatePurchaseOrdersFromShortageCommand command)
    {
        var result = await _mediator.Send(command).ConfigureAwait(false);
        if (!result.Success)
            return Json(new { success = false, message = LocalizeError(result.ErrorMessage!) });

        return Json(new
        {
            success = true,
            items = result.Items
        });
    }

    [HttpPost]
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
            Items = model.Items?.Where(i => i.ProductId.HasValue && (i.Quantity ?? 0) > 0).Select(i => new CreatePurchaseOrderItemCommand
            {
                ProductId = i.ProductId,
                Quantity = i.Quantity ?? 0,
                UnitCost = i.UnitCost ?? 0
            }).ToList() ?? []
        });

        if (!result.Success)
        {
            AddLocalizedModelError(result.ErrorMessage);
            model = await _purchaseOrderModelFactory.PrepareCreatePurchaseOrderModel(model);
            return View(model);
        }

        //await _mediator.Send(new SubmitsPurchaseOrderCommand
        //{
        //    PurchaseOrderId = result.CreatedId!.Value
        //});

        //await _mediator.Send(new ChangePurchaseOrderStatusCommand
        //{
        //    PurchaseOrderId = result.CreatedId!.Value,
        //    Status = (int)PurchaseOrderStatus.Approved
        //});

        NotifySuccess("Msg.SaveSuccess");
        return RedirectToAction(nameof(Details), new { id = result.CreatedId });
    }

    [HttpPost]
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
            Note = model.Note
        });

        if (!result.Success)
            return Json(new { success = false, message = LocalizeError(result.ErrorMessage!) });
        return Json(new { success = true, message = string.Empty });
    }

    [HttpPost]
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

        var result = await _mediator.Send(new ReceivePurchaseOrderItemCommand
        {
            PurchaseOrderId = model.PurchaseOrderId,
            PurchaseOrderItemId = model.PurchaseOrderItemId,
            ReceivedQuantity = model.ReceivedQuantity,
            WarehouseId = model.WarehouseId,
            SellingPrice = model.SellingPrice,
            OversupplyAction = model.OversupplyAction
        });

        if (!result.Success)
        {
            NotifyError(result.ErrorMessage!);
            return RedirectToAction(nameof(Details), new { id = model.PurchaseOrderId });
        }

        if (model.DirectShipOrderItemId.HasValue && model.DirectShipOrderItemId != Guid.Empty
            && !string.IsNullOrWhiteSpace(model.DirectShipAddress))
        {
            var dsResult = await _purchaseOrderAppService.AllocatePoItemToOrderAsync(new AllocatePoItemToOrderAppDto
            {
                PurchaseOrderItemId = model.PurchaseOrderItemId,
                OrderItemId = model.DirectShipOrderItemId.Value,
                Quantity = model.ReceivedQuantity,
                DirectShipAddress = model.DirectShipAddress,
                DirectShipContactName = model.DirectShipContactName,
                DirectShipContactPhone = model.DirectShipContactPhone
            }).ConfigureAwait(false);

            if (!dsResult.Success)
                NotifyWarning(dsResult.ErrorMessage!);
        }

        NotifySuccess("Msg.SaveSuccess");
        return RedirectToAction(nameof(Details), new { id = model.PurchaseOrderId });
    }

    [HttpPost]
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
                ActualUnitCost = line.ActualUnitCost
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

            var receivedItemIds = lines.Select(l => l.ItemId).ToHashSet();
            foreach (var line in model.Items ?? [])
            {
                if (!receivedItemIds.Contains(line.ItemId)) continue;
                if (!line.DirectShipOrderItemId.HasValue || string.IsNullOrWhiteSpace(line.DirectShipAddress)) continue;

                var dsResult = await _purchaseOrderAppService.AllocatePoItemToOrderAsync(new AllocatePoItemToOrderAppDto
                {
                    PurchaseOrderItemId = line.ItemId,
                    OrderItemId = line.DirectShipOrderItemId.Value,
                    Quantity = line.Quantity,
                    DirectShipAddress = line.DirectShipAddress,
                    DirectShipContactName = line.DirectShipContactName,
                    DirectShipContactPhone = line.DirectShipContactPhone
                });

                if (!dsResult.Success)
                    NotifyWarning(dsResult.ErrorMessage!);
            }
        }

        return RedirectToAction(nameof(Details), new { id = model.PurchaseOrderId });
    }

    [HttpPost]
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
    public async Task<IActionResult> ChangeStatus(Guid id, int status)
    {
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
    public async Task<IActionResult> EligibleOrderItems(Guid purchaseOrderItemId)
    {
        if (purchaseOrderItemId == Guid.Empty)
            return Json(Array.Empty<object>());

        var items = await _purchaseOrderAppService.GetEligibleOrderItemsForPoItemAsync(purchaseOrderItemId).ConfigureAwait(false);

        return Json(items.Select(item => new
        {
            orderItemId = item.OrderItemId,
            orderId = item.OrderId,
            orderCode = item.OrderCode,
            customerName = item.CustomerName,
            customerPhone = item.CustomerPhone,
            shippingAddress = item.ShippingAddress,
            productName = item.ProductName,
            totalQuantity = item.TotalQuantity,
            allocatedOutstanding = item.AllocatedOutstanding,
            availableToAllocate = item.AvailableToAllocate
        }));
    }

    [HttpPost]
    public async Task<IActionResult> AllocateToOrder([FromBody] AllocateToOrderRequest request)
    {
        if (request is null || request.PurchaseOrderItemId == Guid.Empty || request.OrderItemId == Guid.Empty || request.Quantity <= 0)
            return Json(new { success = false, message = LocalizeError("Error.InvalidRequest") });

        var result = await _purchaseOrderAppService.AllocatePoItemToOrderAsync(new AllocatePoItemToOrderAppDto
        {
            PurchaseOrderItemId = request.PurchaseOrderItemId,
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
}

public sealed class AllocateToOrderRequest
{
    public Guid PurchaseOrderItemId { get; set; }
    public Guid OrderItemId { get; set; }
    public decimal Quantity { get; set; }
    public string? DirectShipAddress { get; set; }
    public string? DirectShipContactName { get; set; }
    public string? DirectShipContactPhone { get; set; }
}
