using MediatR;
using Microsoft.AspNetCore.Mvc;
using NamEcommerce.Web.Contracts.Commands.Models.PurchaseOrders;
using NamEcommerce.Web.Services.PurchaseOrders;
using NamEcommerce.Web.Models.Catalog;
using NamEcommerce.Web.Models.PurchaseOrders;
using NamEcommerce.Web.Contracts.Queries.Models.PurchaseOrders;

namespace NamEcommerce.Web.Controllers;

public sealed class PurchaseOrderController : BaseAuthorizedController
{
    private readonly IMediator _mediator;
    private readonly IPurchaseOrderModelFactory _purchaseOrderModelFactory;

    public PurchaseOrderController(IMediator mediator, IPurchaseOrderModelFactory purchaseOrderModelFactory)
    {
        _mediator = mediator;
        _purchaseOrderModelFactory = purchaseOrderModelFactory;
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
    [HttpPost]
    public async Task<IActionResult> Create(CreatePurchaseOrderModel model)
    {
        if (!ModelState.IsValid)
        {
            model = await _purchaseOrderModelFactory.PrepareCreatePurchaseOrderModel(model);
            return View(model);
        }

        var result = await _mediator.Send(new CreatePurchaseOrderCommand
        {
            PlacedOn = model.PlacedOn,
            VendorId = model.VendorId,
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
            SellingPrice = model.SellingPrice
        });

        if (!result.Success)
            NotifyError(result.ErrorMessage!);
        else
            NotifySuccess("Msg.SaveSuccess");

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
}
