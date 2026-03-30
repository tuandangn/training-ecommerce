using MediatR;
using Microsoft.AspNetCore.Mvc;
using NamEcommerce.Web.Constants;
using NamEcommerce.Web.Contracts.Commands.Models.Inventory;
using NamEcommerce.Web.Contracts.Configurations;
using NamEcommerce.Web.Contracts.Queries.Models.Inventory;
using NamEcommerce.Web.Models.Inventory;
using NamEcommerce.Web.Contracts.Services;

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
}
