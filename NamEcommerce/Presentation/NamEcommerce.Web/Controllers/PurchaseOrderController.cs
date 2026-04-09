using MediatR;
using Microsoft.AspNetCore.Mvc;
using NamEcommerce.Web.Constants;
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
            VendorId = model.VendorId,
            WarehouseId = model.WarehouseId,
            ShippingAmount = 0,
            TaxAmount = 0,
            Note = model.Note,
            ExpectedDeliveryDate = model.ExpectedDeliveryDate
        });

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage!);
            return View(model);
        }
        return RedirectToAction(nameof(Details), new { id = result.CreatedId });
    }

    [HttpPost]
    public async Task<IActionResult> Edit(EditPurchaseOrderModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData[ViewConstants.PurchaseOrderErrorMessage] = "Dữ liệu không hợp lệ";
            return RedirectToAction(nameof(Details), new { id = model.Id });
        }

        var purchaseOrder = await _mediator.Send(new GetPurchaseOrderQuery { Id = model.Id });
        if (purchaseOrder is null)
        {
            TempData[ViewConstants.PurchaseOrderErrorMessage] = "Không tìm thấy đơn nhập hàng.";
            return RedirectToAction(nameof(List));
        }

        var updatePurchaseOrderResult = await _mediator.Send(new UpdatePurchaseOrderCommand
        {
            Id = purchaseOrder.Id,
            ShippingAmount = model.ShippingAmount,
            TaxAmount = model.TaxAmount,
            VendorId = model.VendorId,
            ExpectedDeliveryDate = model.ExpectedDeliveryDate,
            Note = model.Note
        });

        if (!updatePurchaseOrderResult.Success)
        {
            TempData[ViewConstants.PurchaseOrderErrorMessage] = updatePurchaseOrderResult.ErrorMessage;
            return RedirectToAction(nameof(Details), new { id = model.Id });
        }
        TempData[ViewConstants.PurchaseOrderSuccessMessage] = "Chỉnh sửa đơn nhập hàng thành công!";
        return RedirectToAction(nameof(Details), new { id = model.Id });
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var model = await _purchaseOrderModelFactory.PreparePurchaseOrderDetailsModel(id);
        if (model == null)
        {
            TempData[ViewConstants.PurchaseOrderErrorMessage] = "Không tìm thấy đơn nhập hàng.";
            return RedirectToAction(nameof(List));
        }

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> AddPurchaseOrderItem(AddPurchaseOrderItemModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData[ViewConstants.PurchaseOrderAddItemErrorMessage] = "Dữ liệu không hợp lệ";
            return RedirectToAction(nameof(Details), new { id = model.PurchaseOrderId });
        }

        var purchaseOrder = await _mediator.Send(new GetPurchaseOrderQuery { Id = model.PurchaseOrderId });
        if (purchaseOrder is null)
        {
            TempData[ViewConstants.PurchaseOrderErrorMessage] = "Không tìm thấy đơn nhập hàng.";
            return RedirectToAction(nameof(List));
        }

        var result = await _mediator.Send(new AddPurchaseOrderItemCommand
        {
            PurchaseOrderId = model.PurchaseOrderId,
            ProductId = model.ProductId ?? default,
            Quantity = model.Quantity ?? 0,
            UnitCost = model.UnitCost ?? 0,
            Note = model.Note
        });

        if (!result.Success)
            TempData[ViewConstants.PurchaseOrderErrorMessage] = result.ErrorMessage;
        else
            TempData[ViewConstants.PurchaseOrderSuccessMessage] = "Thêm sản phẩm thành công!";

        return RedirectToAction(nameof(Details), new { id = model.PurchaseOrderId });
    }
    [HttpPost]
    public async Task<IActionResult> Receive(ReceivePurchaseOrderItemModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData[ViewConstants.PurchaseOrderErrorMessage] = "Dữ liệu không hợp lệ";
            return RedirectToAction(nameof(Details), new { id = model.PurchaseOrderId });
        }

        var purchaseOrder = await _mediator.Send(new GetPurchaseOrderQuery { Id = model.PurchaseOrderId });
        if (purchaseOrder is null)
        {
            TempData[ViewConstants.PurchaseOrderErrorMessage] = "Không tìm thấy đơn nhập hàng.";
            return RedirectToAction(nameof(List));
        }

        var result = await _mediator.Send(new ReceivePurchaseOrderItemCommand
        {
            PurchaseOrderId = model.PurchaseOrderId,
            PurchaseOrderItemId = model.PurchaseOrderItemId,
            ReceivedQuantity = model.ReceivedQuantity,
            WarehouseId = model.WarehouseId
        });

        if (!result.Success)
            TempData[ViewConstants.PurchaseOrderErrorMessage] = result.ErrorMessage;
        else
            TempData[ViewConstants.PurchaseOrderSuccessMessage] = $"Nhập kho {model.ReceivedQuantity.ToString(ViewConstants.NumberCustomFormat)} sản phẩm thành công!";

        return RedirectToAction(nameof(Details), new { id = model.PurchaseOrderId });
    }

    [HttpPost]
    public async Task<IActionResult> SubmitsPurchaseOrder(Guid id)
    {
        var purchaseOrder = await _mediator.Send(new GetPurchaseOrderQuery { Id = id });
        if (purchaseOrder is null)
        {
            TempData[ViewConstants.PurchaseOrderErrorMessage] = "Không tìm thấy đơn nhập hàng.";
            return RedirectToAction(nameof(List));
        }

        var (success, errorMessage) = await _mediator.Send(new SubmitsPurchaseOrderCommand
        {
            PurchaseOrderId = id
        });

        TempData[success ? ViewConstants.PurchaseOrderSuccessMessage : ViewConstants.PurchaseOrderErrorMessage] = errorMessage;
        return RedirectToAction(nameof(Details), new { id });
    }
    [HttpPost]
    public async Task<IActionResult> CancelPurchaseOrder(Guid id)
    {
        var purchaseOrder = await _mediator.Send(new GetPurchaseOrderQuery { Id = id });
        if (purchaseOrder is null)
        {
            TempData[ViewConstants.PurchaseOrderErrorMessage] = "Không tìm thấy đơn nhập hàng.";
            return RedirectToAction(nameof(List));
        }

        var (success, errorMessage) = await _mediator.Send(new CancelPurchaseOrderCommand
        {
            PurchaseOrderId = id
        });

        TempData[success ? ViewConstants.PurchaseOrderSuccessMessage : ViewConstants.PurchaseOrderErrorMessage] = errorMessage;
        return RedirectToAction(nameof(Details), new { id });
    }
    [HttpPost]
    public async Task<IActionResult> ChangeStatus(Guid id, int status)
    {
        var purchaseOrder = await _mediator.Send(new GetPurchaseOrderQuery { Id = id });
        if (purchaseOrder is null)
        {
            TempData[ViewConstants.PurchaseOrderErrorMessage] = "Không tìm thấy đơn nhập hàng.";
            return RedirectToAction(nameof(List));
        }

        var result = await _mediator.Send(new ChangePurchaseOrderStatusCommand
        {
            PurchaseOrderId = id,
            Status = status
        });

        if (!result.Success)
            TempData[ViewConstants.PurchaseOrderErrorMessage] = result.ErrorMessage;
        else
            TempData[ViewConstants.PurchaseOrderSuccessMessage] = "Cập nhật trạng thái thành công!";

        return RedirectToAction(nameof(Details), new { id });
    }
}
