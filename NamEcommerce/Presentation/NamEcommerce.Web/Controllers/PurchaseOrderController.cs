using MediatR;
using Microsoft.AspNetCore.Mvc;
using NamEcommerce.Web.Constants;
using NamEcommerce.Web.Contracts.Commands.Models.Inventory;
using NamEcommerce.Web.Contracts.Configurations;
using NamEcommerce.Web.Contracts.Queries.Models.Inventory;
using NamEcommerce.Web.Contracts.Queries.Models.Catalog;
using NamEcommerce.Web.Contracts.Services;
using NamEcommerce.Domain.Shared.Enums.Inventory;
using NamEcommerce.Web.Contracts.Queries.Models.PurchaseOrders;
using NamEcommerce.Web.Contracts.Commands.Models.PurchaseOrders;

namespace NamEcommerce.Web.Controllers;

public sealed class PurchaseOrderController : BaseAuthorizedController
{
    private readonly AppConfig _appConfig;
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUserService;

    public PurchaseOrderController(AppConfig appConfig, IMediator mediator, ICurrentUserService currentUserService)
    {
        _appConfig = appConfig;
        _mediator = mediator;
        _currentUserService = currentUserService;
    }

    public IActionResult Index() => RedirectToAction(nameof(List));

    public async Task<IActionResult> List(string? keywords, int pageNumber = 1)
    {
        var model = await _mediator.Send(new GetPurchaseOrderListQuery
        {
            Keywords = keywords,
            PageIndex = pageNumber - 1,
            PageSize = _appConfig.DefaultPageSize
        });
        return View(model);
    }

    public async Task<IActionResult> Create()
    {
        ViewBag.AvailableVendors = await _mediator.Send(new GetVendorOptionListQuery());
        ViewBag.AvailableWarehouses = await _mediator.Send(new GetWarehouseOptionListQuery());
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreatePurchaseOrderInputModel input)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.AvailableVendors = await _mediator.Send(new GetVendorOptionListQuery());
            ViewBag.AvailableWarehouses = await _mediator.Send(new GetWarehouseOptionListQuery());
            return View(input);
        }

        var currentUser = await _currentUserService.GetCurrentUserInfoAsync();

        var result = await _mediator.Send(new CreatePurchaseOrderCommand
        {
            VendorId = input.VendorId,
            WarehouseId = input.WarehouseId,
            Note = input.Note,
            ExpectedDeliveryDate = input.ExpectedDeliveryDate,
            CreatedByUserId = currentUser?.Id ?? Guid.Empty
        });

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage!);
            return View(input);
        }

        TempData[ViewConstants.WarehouseSuccessMessage] = "Tạo đơn nhập hàng thành công!";
        return RedirectToAction(nameof(Details), new { id = result.CreatedId });
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var model = await _mediator.Send(new GetPurchaseOrderQuery { Id = id });
        if (model == null)
        {
            TempData[ViewConstants.WarehouseErrorMessage] = "Không tìm thấy đơn nhập hàng.";
            return RedirectToAction(nameof(List));
        }

        ViewBag.AvailableProducts = await _mediator.Send(new GetProductOptionListQuery());
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> AddItem(AddPurchaseOrderItemInputModel input)
    {
        if (!ModelState.IsValid)
            return RedirectToAction(nameof(Details), new { id = input.PurchaseOrderId });

        var result = await _mediator.Send(new AddPurchaseOrderItemCommand
        {
            PurchaseOrderId = input.PurchaseOrderId,
            ProductId = input.ProductId,
            Quantity = input.Quantity,
            UnitCost = input.UnitCost,
            Note = input.Note
        });

        if (!result.Success)
            TempData[ViewConstants.WarehouseErrorMessage] = result.ErrorMessage;
        else
            TempData[ViewConstants.WarehouseSuccessMessage] = "Thêm sản phẩm thành công!";

        return RedirectToAction(nameof(Details), new { id = input.PurchaseOrderId });
    }

    [HttpPost]
    public async Task<IActionResult> Receive(ReceivePurchaseOrderInputModel input)
    {
        var currentUser = await _currentUserService.GetCurrentUserInfoAsync();

        var result = await _mediator.Send(new ReceivePurchaseOrderItemCommand
        {
            PurchaseOrderId = input.PurchaseOrderId,
            PurchaseOrderItemId = input.PurchaseOrderItemId,
            ReceivedQuantity = input.ReceivedQuantity,
            ReceivedByUserId = currentUser?.Id ?? Guid.Empty
        });

        if (!result.Success)
            TempData[ViewConstants.WarehouseErrorMessage] = result.ErrorMessage;
        else
            TempData[ViewConstants.WarehouseSuccessMessage] = $"Nhập kho {input.ReceivedQuantity:N0} sản phẩm thành công!";

        return RedirectToAction(nameof(Details), new { id = input.PurchaseOrderId });
    }

    [HttpPost]
    public async Task<IActionResult> ChangeStatus(Guid id, int status)
    {
        var result = await _mediator.Send(new ChangePurchaseOrderStatusCommand
        {
            PurchaseOrderId = id,
            Status = status
        });

        if (!result.Success)
            TempData[ViewConstants.WarehouseErrorMessage] = result.ErrorMessage;
        else
            TempData[ViewConstants.WarehouseSuccessMessage] = "Cập nhật trạng thái thành công!";

        return RedirectToAction(nameof(Details), new { id });
    }
}

// Input models (not stored in DB, just for form)
public sealed class CreatePurchaseOrderInputModel
{
    public required Guid VendorId { get; set; }
    public required Guid WarehouseId { get; set; }
    public string? Note { get; set; }
    public DateTime? ExpectedDeliveryDate { get; set; }
}

public sealed class AddPurchaseOrderItemInputModel
{
    public required Guid PurchaseOrderId { get; set; }
    public required Guid ProductId { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitCost { get; set; }
    public string? Note { get; set; }
}

public sealed class ReceivePurchaseOrderInputModel
{
    public required Guid PurchaseOrderId { get; set; }
    public required Guid ProductId { get; set; }
    public required Guid PurchaseOrderItemId { get; set; }
    public decimal ReceivedQuantity { get; set; }
}
