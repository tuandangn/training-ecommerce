using MediatR;
using Microsoft.AspNetCore.Mvc;
using NamEcommerce.Web.Constants;
using NamEcommerce.Web.Contracts.Commands.Models.Inventory;
using NamEcommerce.Web.Contracts.Queries.Models.Inventory;
using NamEcommerce.Web.Models.Inventory;
using NamEcommerce.Web.Services.Inventory;

namespace NamEcommerce.Web.Controllers;

public sealed class WarehouseController : BaseAuthorizedController
{
    private readonly IMediator _mediator;
    private readonly IWarehouseModelFactory _warehouseModelFactory;

    public WarehouseController(IMediator mediator, IWarehouseModelFactory warehouseModelFactory)
    {
        _mediator = mediator;
        _warehouseModelFactory = warehouseModelFactory;
    }

    public IActionResult Index() => RedirectToAction(nameof(List));

    public async Task<IActionResult> List(WarehouseListSearchModel searchModel)
    {
        var model = await _warehouseModelFactory.PrepareWarehouseListModel(searchModel);

        return View(model);
    }

    public async Task<IActionResult> Create()
    {
        var model = await _warehouseModelFactory.PrepareCreateWarehouseModel();
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateWarehouseModel model)
    {
        if (!ModelState.IsValid)
        {
            model = await _warehouseModelFactory.PrepareCreateWarehouseModel(model);
            return View(model);
        }

        var createWarehouseResult = await _mediator.Send(new CreateWarehouseCommand
        {
            Name = model.Name!,
            Code = model.Code!,
            WarehouseType = model.WarehouseType,
            Address = model.Address,
            PhoneNumber = model.PhoneNumber,
            IsActive = model.IsActive
        });
        if (!createWarehouseResult.Success)
        {
            ModelState.AddModelError(string.Empty, createWarehouseResult.ErrorMessage!);
            model = await _warehouseModelFactory.PrepareCreateWarehouseModel(model);
            return View(model);
        }

        TempData[ViewConstants.WarehouseSuccessMessage] = "Thêm mới kho hàng thành công!";
        return RedirectToAction(nameof(List));
    }

    public async Task<IActionResult> Edit(Guid id)
    {
        var model = await _warehouseModelFactory.PrepareEditWarehouseModel(id);
        if (model == null)
        {
            TempData[ViewConstants.WarehouseErrorMessage] = "Không tìm thấy kho hàng.";
            return RedirectToAction(nameof(List));
        }

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(EditWarehouseModel model)
    {
        if (!ModelState.IsValid)
        {
            model = (await _warehouseModelFactory.PrepareEditWarehouseModel(model.Id, model))!;
            return View(model);
        }

        var warehouse = await _mediator.Send(new GetWarehouseQuery { Id = model.Id });
        if (warehouse == null)
        {
            TempData[ViewConstants.WarehouseErrorMessage] = "Không tìm thấy kho hàng.";
            return RedirectToAction(nameof(List));
        }

        var updateWarehouseResult = await _mediator.Send(new UpdateWarehouseCommand
        {
            Id = model.Id,
            Code = model.Code!,
            Name = model.Name!,
            WarehouseType = model.WarehouseType,
            PhoneNumber = model.PhoneNumber,
            Address = model.Address,
            IsActive = model.IsActive
        });
        if (!updateWarehouseResult.Success)
        {
            ModelState.AddModelError(string.Empty, updateWarehouseResult.ErrorMessage!);
            model = (await _warehouseModelFactory.PrepareEditWarehouseModel(model.Id, model))!;
            return View(model);
        }

        TempData[ViewConstants.WarehouseSuccessMessage] = "Chỉnh sửa kho hàng thành công!";
        return RedirectToAction(nameof(List));
    }

    [HttpPost]
    public async Task<IActionResult> Delete(Guid id)
    {
        var resultDto = await _mediator.Send(new DeleteWarehouseCommand(id));

        if (!resultDto.Success)
            TempData[ViewConstants.WarehouseErrorMessage] = resultDto.ErrorMessage;
        else 
            TempData[ViewConstants.WarehouseSuccessMessage] = "Xóa kho hàng thành công!";
        return RedirectToAction(nameof(List));
    }
}
