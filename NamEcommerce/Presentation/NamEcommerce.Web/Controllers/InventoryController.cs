using MediatR;
using Microsoft.AspNetCore.Mvc;
using NamEcommerce.Web.Contracts.Commands.Models.Inventory;
using NamEcommerce.Web.Contracts.Configurations;
using NamEcommerce.Web.Contracts.Queries.Models.Inventory;
using NamEcommerce.Web.Models.Inventory;

namespace NamEcommerce.Web.Controllers;

public sealed class InventoryController : BaseAuthorizedController
{
    private readonly AppConfig _appConfig;
    private readonly IMediator _mediator;

    public InventoryController(AppConfig appConfig, IMediator mediator)
    {
        _appConfig = appConfig;
        _mediator = mediator;
    }

    public IActionResult Index() => RedirectToAction(nameof(StockList));

    public async Task<IActionResult> StockList(int pageNumber = 1, string? keywords = null)
    {
        var pageSize = _appConfig.DefaultPageSize;

        var model = await _mediator.Send(new GetInventoryStockListQuery
        {
            Keywords = keywords,
            WarehouseId = null,
            PageIndex = pageNumber - 1,
            PageSize = pageSize
        });

        return View(model);
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

    public async Task<IActionResult> ReservationLedger(Guid productId, int pageNumber = 1)
    {
        var pageSize = _appConfig.DefaultPageSize;

        var model = await _mediator.Send(new GetProductReservationLedgerQuery
        {
            ProductId = productId,
            PageIndex = pageNumber - 1,
            PageSize = pageSize
        });

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> SetStockLevels(SetStockLevelsModel model)
    {
        if (!ModelState.IsValid)
        {
            var errors = string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
            return Json(new { success = false, errorMessage = errors });
        }

        var result = await _mediator.Send(new SetStockLevelsCommand
        {
            Id = model.Id,
            ReorderLevel = model.ReorderLevel,
            MaxStockLevel = model.MaxStockLevel
        });

        return Json(new { success = result.Success, errorMessage = result.ErrorMessage });
    }
}
