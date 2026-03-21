using MediatR;
using Microsoft.AspNetCore.Mvc;
using NamEcommerce.Web.Common;
using NamEcommerce.Web.Contracts.Commands.Models.Catalog;
using NamEcommerce.Web.Contracts.Queries.Models.Catalog;
using NamEcommerce.Web.Models.Catalog;

namespace NamEcommerce.Web.Controllers;

public sealed class UnitMeasurementController : BaseAuthorizedController
{
    private readonly AppConfig _appConfig;
    private readonly IMediator _mediator;

    public UnitMeasurementController(AppConfig appConfig, IMediator mediator)
    {
        _appConfig = appConfig;
        _mediator = mediator;
    }

    public IActionResult Index() => RedirectToAction(nameof(List));

    public IActionResult List(UnitMeasurementListSearchModel searchModel)
    {
        var pageNumber = searchModel?.PageNumber ?? 1;
        var pageSize = searchModel?.PageSize ?? 0;
        if (pageNumber <= 0) pageNumber = 1;
        if (pageSize <= 0) pageSize = _appConfig.DefaultPageSize;
        if(_appConfig.PageSizeOptions.Contains(pageSize)) pageSize = _appConfig.DefaultPageSize;

        var model = _mediator.Send(new GetUnitMeasurementListQuery
        {
            Keywords = searchModel?.Keywords,
            PageIndex = pageNumber - 1,
            PageSize = pageSize
        }).Result;

        return View(model);
    }

    public IActionResult Create()
    {
        var model = new CreateUnitMeasurementModel
        {
            DisplayOrder = 1
        };
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateUnitMeasurementModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var createUnitMeasurementResult = await _mediator.Send(new CreateUnitMeasurementCommand
        {
            Name = model.Name!,
            DisplayOrder = model.DisplayOrder
        });
        if (!createUnitMeasurementResult.Success)
        {
            ModelState.AddModelError(string.Empty, createUnitMeasurementResult.ErrorMessage!);
            return View(model);
        }

        TempData[ViewConstants.UnitMeasurementSuccessMessage] = "Thêm mới đơn vị tính thành công!";
        return RedirectToAction(nameof(List));
    }

    public async Task<IActionResult> Edit(Guid id)
    {
        var unitMeasurement = await _mediator.Send(new GetUnitMeasurementQuery { Id = id });
        if (unitMeasurement == null)
            return RedirectToAction(nameof(List));

        var model = new EditUnitMeasurementModel
        {
            Id = unitMeasurement.Id,
            Name = unitMeasurement.Name,
            DisplayOrder = unitMeasurement.DisplayOrder
        };

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(EditUnitMeasurementModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var unitMeasurement = await _mediator.Send(new GetUnitMeasurementQuery { Id = model.Id });
        if (unitMeasurement == null)
            return RedirectToAction(nameof(List));

        var updateUnitMeasurementResult = await _mediator.Send(new UpdateUnitMeasurementCommand
        {
            Id = model.Id,
            Name = model.Name!,
            DisplayOrder = model.DisplayOrder
        });
        if (!updateUnitMeasurementResult.Success)
        {
            ModelState.AddModelError(string.Empty, updateUnitMeasurementResult.ErrorMessage!);
            return View(model);
        }

        TempData[ViewConstants.UnitMeasurementSuccessMessage] = "Chỉnh sửa đơn vị tính thành công!";
        return RedirectToAction(nameof(List));
    }

    [HttpPost]
    public async Task<IActionResult> Delete(Guid id)
    {
        var resultDto = await _mediator.Send(new DeleteUnitMeasurementCommand(id));
        if (!resultDto.Success)
            TempData[ViewConstants.UnitMeasurementErrorMessage] = resultDto.ErrorMessage;

        TempData[ViewConstants.UnitMeasurementSuccessMessage] = "Xóa đơn vị tính thành công!";
        return RedirectToAction(nameof(List));
    }
}
