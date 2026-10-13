using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NamEcommerce.Application.Contracts.Dtos.Inventory;
using NamEcommerce.Application.Contracts.Inventory;
using NamEcommerce.Domain.Shared.Exceptions;
using NamEcommerce.Web.Contracts.Commands.Models.Inventory;
using NamEcommerce.Web.Contracts.Configurations;
using NamEcommerce.Web.Contracts.Models.Inventory;
using NamEcommerce.Web.Contracts.Queries.Models.Inventory;
using NamEcommerce.Web.Contracts.Security;
using NamEcommerce.Web.Extensions;
using NamEcommerce.Web.Framework.Services;
using NamEcommerce.Web.Models.Inventory;

namespace NamEcommerce.Web.Controllers;

public sealed class InventoryController(AppConfig appConfig, IMediator mediator, IInventoryAppService inventoryAppService) : BaseAuthorizedController
{
    public IActionResult Index() => RedirectToAction(nameof(StockList));

    [Authorize(Policy = SystemPermissions.Inventory.View)]
    public async Task<IActionResult> StockList(int pageNumber = 1, string? keywords = null, bool includeDS = false, bool groupByProduct = true)
    {
        var pageSize = appConfig.DefaultPageSize;

        var model = await mediator.Send(new GetInventoryStockListQuery
        {
            Keywords = keywords,
            WarehouseId = null,
            IncludeDirectTransit = includeDS,
            GroupByProduct = groupByProduct,
            PageIndex = pageNumber - 1,
            PageSize = pageSize
        });

        return View(model);
    }

    [Authorize(Policy = SystemPermissions.Inventory.ViewCostDetails)]
    public async Task<IActionResult> CostHistory(Guid productId, Guid? warehouseId = null, int take = 20)
    {
        var items = await inventoryAppService.GetInventoryCostHistoryAsync(productId, warehouseId, take);
        return PartialView("_InventoryCostHistoryFullTable", items.Select(MapCostHistory).ToList());
    }

    [Authorize(Policy = SystemPermissions.Inventory.View)]
    public async Task<IActionResult> MovementLogs(Guid productId, Guid warehouseId, int pageNumber = 1)
    {
        var pageSize = appConfig.DefaultPageSize;

        var model = await mediator.Send(new GetStockMovementLogsQuery
        {
            ProductId = productId,
            WarehouseId = warehouseId,
            PageIndex = pageNumber - 1,
            PageSize = pageSize
        });

        return View(model);
    }

    [Authorize(Policy = SystemPermissions.Inventory.View)]
    public async Task<IActionResult> ReservationLedger(Guid id, int pageNumber = 1)
    {
        var pageSize = appConfig.DefaultPageSize;

        var model = await mediator.Send(new GetProductReservationLedgerQuery
        {
            ProductId = id,
            PageIndex = pageNumber - 1,
            PageSize = pageSize
        });

        return View(model);
    }

    [Authorize(Policy = SystemPermissions.Inventory.ViewCostDetails)]
    public async Task<IActionResult> CostingPolicy()
    {
        var settings = await mediator.Send(new GetInventoryCostingPolicyQuery());
        return View(InventoryCostingPolicyModel.FromSettings(settings));
    }

    [HttpPost]
    [Authorize(Policy = SystemPermissions.Inventory.ViewCostDetails)]
    public async Task<IActionResult> CostingPolicy(InventoryCostingPolicyModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        try
        {
            var result = await mediator.Send(new UpdateInventoryCostingPolicyCommand
            {
                CostingMethod = (int)model.CostingMethod,
                ValuationScope = (int)model.ValuationScope,
                EffectiveFrom = model.EffectiveFrom,
                Note = model.Note
            });

            if (!result.Success)
            {
                AddLocalizedModelError(result.ErrorMessage);
                return View(model);
            }

            NotificationService.Success("Đã lưu cài đặt giá vốn.");
            return RedirectToAction(nameof(CostingPolicy));
        }
        catch (NamEcommerceDomainException ex)
        {
            AddLocalizedModelError(ex.ErrorCode, ex.Parameters);
            return View(model);
        }
    }

    [HttpPost]
    [Authorize(Policy = SystemPermissions.Inventory.ViewCostDetails)]
    public async Task<IActionResult> RebuildCosting(InventoryCostingPolicyModel model)
    {
        try
        {
            var result = await mediator.Send(new RebuildInventoryCostingCommand
            {
                CostingMethod = (int)model.CostingMethod,
                ValuationScope = (int)model.ValuationScope
            });

            if (!result.Success)
            {
                NotificationService.Error(string.IsNullOrEmpty(result.ErrorMessage) ? "Không thể tính lại giá vốn." : LocalizeError(result.ErrorMessage));
                return RedirectToAction(nameof(CostingPolicy));
            }

            NotificationService.Success($"Đã tính lại giá vốn. Mã lần tính: {result.RebuildRunId}");
            return RedirectToAction(nameof(CostingPolicy));
        }
        catch (NamEcommerceDomainException ex)
        {
            NotificationService.Error(LocalizeError(ex.ErrorCode, ex.Parameters));
            return RedirectToAction(nameof(CostingPolicy));
        }
    }

    [HttpPost]
    [Authorize(Policy = SystemPermissions.Inventory.Adjust)]
    public async Task<IActionResult> SetStockLevels(SetStockLevelsModel model)
    {
        if (!ModelState.IsValid)
            return this.JsonError(GetErrorMessage());

        var result = await mediator.Send(new SetStockLevelsCommand
        {
            Id = model.Id,
            ReorderLevel = model.ReorderLevel,
            MaxStockLevel = model.MaxStockLevel
        });
        if (result.Success)
            return this.JsonOk(message: Localizer["Msg.SaveSuccess"]);

        return this.JsonError(result.ErrorMessage!);
    }

    private static InventoryStockListModel.CostHistoryItemModel MapCostHistory(InventoryCostHistoryAppDto item)
        => new(item.Id)
        {
            ProductId = item.ProductId,
            ProductName = item.ProductName,
            WarehouseId = item.WarehouseId,
            WarehouseName = item.WarehouseName,
            OccurredAt = DateTimeHelper.ToLocalTime(item.OccurredAtUtc),
            SequenceNumber = item.SequenceNumber,
            MovementType = item.MovementType,
            QuantityDelta = item.QuantityDelta,
            UnitCost = item.UnitCost,
            TotalCost = item.TotalCost,
            QuantityBalanceAfter = item.QuantityBalanceAfter,
            ValueBalanceAfter = item.ValueBalanceAfter,
            AverageCostAfter = item.AverageCostAfter,
            CostingStatus = item.CostingStatus,
            ReferenceType = item.ReferenceType,
            ReferenceId = item.ReferenceId,
            ReferenceItemId = item.ReferenceItemId
        };
}
