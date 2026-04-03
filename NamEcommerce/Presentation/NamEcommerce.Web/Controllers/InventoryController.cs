using MediatR;
using Microsoft.AspNetCore.Mvc;
using NamEcommerce.Web.Constants;
using NamEcommerce.Web.Contracts.Commands.Models.Inventory;
using NamEcommerce.Web.Contracts.Configurations;
using NamEcommerce.Web.Contracts.Queries.Models.Inventory;
using NamEcommerce.Web.Models.Inventory;
using NamEcommerce.Web.Contracts.Services;
using NamEcommerce.Web.Contracts.Models.Inventory;

namespace NamEcommerce.Web.Controllers;

public sealed class InventoryController : BaseAuthorizedController
{
    private readonly AppConfig _appConfig;
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUserService;

    public InventoryController(AppConfig appConfig, IMediator mediator, ICurrentUserService currentUserService)
    {
        _appConfig = appConfig;
        _mediator = mediator;
        _currentUserService = currentUserService;
    }

    public IActionResult Index() => RedirectToAction(nameof(StockList));

    public async Task<IActionResult> StockList(int pageNumber = 1)
    {
        var pageSize = _appConfig.DefaultPageSize;

        var model = await _mediator.Send(new GetInventoryStockListQuery
        {
            Keywords = null,
            WarehouseId = null,
            PageIndex = pageNumber - 1,
            PageSize = pageSize
        });

        return View(model);
    }

    public async Task<IActionResult> AdjustStock(Guid productId, Guid warehouseId)
    {
        // Simple form
        var model = new AdjustStockModel
        {
            ProductId = productId,
            WarehouseId = warehouseId
        };
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> AdjustStock(AdjustStockModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var currentUser = await _currentUserService.GetCurrentUserInfoAsync();

        var result = await _mediator.Send(new AdjustStockCommand
        {
            ProductId = model.ProductId,
            WarehouseId = model.WarehouseId,
            NewQuantity = model.NewQuantity,
            Note = model.Note,
            ModifiedByUserId = currentUser?.Id ?? Guid.Empty
        });

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage!);
            return View(model);
        }

        TempData[ViewConstants.VendorSuccessMessage] = "Điều chỉnh tồn kho thành công!";
        return RedirectToAction(nameof(StockList));
    }

    public async Task<IActionResult> MovementLogs(Guid productId, Guid warehouseId, int pageNumber = 1)
    {
        var pageSize = _appConfig.DefaultPageSize;

        var model = await _mediator.Send(new GetStockMovementLogsQuery
        {
            ProductId = productId,
            WarehouseId = warehouseId,
            PageIndex = pageNumber - 1,
            PageSize = pageSize
        });

        return View(model);
    }

    #region Reserve Stock

    public async Task<IActionResult> ReserveStock(Guid productId, Guid warehouseId)
    {
        var model = new ReserveStockModel
        {
            ProductId = productId,
            WarehouseId = warehouseId
        };
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> ReserveStock(ReserveStockModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var currentUser = await _currentUserService.GetCurrentUserInfoAsync();

        var result = await _mediator.Send(new ReserveStockCommand
        {
            ProductId = model.ProductId,
            WarehouseId = model.WarehouseId,
            Quantity = model.Quantity,
            ReferenceId = model.ReferenceId,
            UserId = currentUser?.Id ?? Guid.Empty,
            Note = model.Note
        });

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage!);
            return View(model);
        }

        TempData[ViewConstants.VendorSuccessMessage] = "Giữ hàng thành công!";
        return RedirectToAction(nameof(StockList));
    }

    #endregion

    #region Release Reserved Stock

    public async Task<IActionResult> ReleaseReservedStock(Guid productId, Guid warehouseId)
    {
        var model = new ReleaseReservedStockModel
        {
            ProductId = productId,
            WarehouseId = warehouseId
        };
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> ReleaseReservedStock(ReleaseReservedStockModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var currentUser = await _currentUserService.GetCurrentUserInfoAsync();

        var result = await _mediator.Send(new ReleaseReservedStockCommand
        {
            ProductId = model.ProductId,
            WarehouseId = model.WarehouseId,
            Quantity = model.Quantity,
            ReferenceId = model.ReferenceId,
            UserId = currentUser?.Id ?? Guid.Empty,
            Note = model.Note
        });

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage!);
            return View(model);
        }

        TempData[ViewConstants.VendorSuccessMessage] = "Giải phóng hàng giữ thành công!";
        return RedirectToAction(nameof(StockList));
    }

    #endregion

    #region Dispatch Stock

    public async Task<IActionResult> DispatchStock(Guid productId, Guid warehouseId)
    {
        var model = new DispatchStockModel
        {
            ProductId = productId,
            WarehouseId = warehouseId
        };
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> DispatchStock(DispatchStockModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var currentUser = await _currentUserService.GetCurrentUserInfoAsync();

        var result = await _mediator.Send(new DispatchStockCommand
        {
            ProductId = model.ProductId,
            WarehouseId = model.WarehouseId,
            Quantity = model.Quantity,
            ReferenceId = model.ReferenceId,
            UserId = currentUser?.Id ?? Guid.Empty,
            Note = model.Note
        });

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage!);
            return View(model);
        }

        TempData[ViewConstants.VendorSuccessMessage] = "Xuất kho thành công!";
        return RedirectToAction(nameof(StockList));
    }

    #endregion

    #region Receive Stock

    public async Task<IActionResult> ReceiveStock(Guid productId, Guid warehouseId)
    {
        var model = new ReceiveStockModel
        {
            ProductId = productId,
            WarehouseId = warehouseId,
            ReferenceType = 0
        };
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> ReceiveStock(ReceiveStockModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var currentUser = await _currentUserService.GetCurrentUserInfoAsync();

        var result = await _mediator.Send(new ReceiveStockCommand
        {
            ProductId = model.ProductId,
            WarehouseId = model.WarehouseId,
            Quantity = model.Quantity,
            ReferenceType = model.ReferenceType,
            ReferenceId = model.ReferenceId,
            UserId = currentUser?.Id ?? Guid.Empty,
            Note = model.Note
        });

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage!);
            return View(model);
        }

        TempData[ViewConstants.VendorSuccessMessage] = "Nhập kho thành công!";
        return RedirectToAction(nameof(StockList));
    }

    #endregion
}
