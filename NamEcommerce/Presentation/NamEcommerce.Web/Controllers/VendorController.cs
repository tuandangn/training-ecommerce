using MediatR;
using Microsoft.AspNetCore.Mvc;
using NamEcommerce.Web.Common;
using NamEcommerce.Web.Contracts.Commands.Models.Catalog;
using NamEcommerce.Web.Contracts.Queries.Models.Catalog;
using NamEcommerce.Web.Models.Catalog;

namespace NamEcommerce.Web.Controllers;

public sealed class VendorController : BaseAuthorizedController
{
    private readonly AppConfig _appConfig;
    private readonly IMediator _mediator;

    public VendorController(AppConfig appConfig, IMediator mediator)
    {
        _appConfig = appConfig;
        _mediator = mediator;
    }

    public IActionResult Index() => RedirectToAction(nameof(List));

    public IActionResult List(VendorListSearchModel searchModel)
    {
        var pageNumber = searchModel?.PageNumber ?? 1;
        var pageSize = searchModel?.PageSize ?? 0;
        if (pageNumber <= 0) pageNumber = 1;
        if (pageSize <= 0) pageSize = _appConfig.DefaultPageSize;
        if (_appConfig.PageSizeOptions.Contains(pageSize)) pageSize = _appConfig.DefaultPageSize;

        var model = _mediator.Send(new GetVendorListQuery
        {
            Keywords = searchModel?.Keywords,
            PageIndex = pageNumber - 1,
            PageSize = pageSize
        }).Result;

        return View(model);
    }

    public IActionResult Create()
    {
        var model = new CreateVendorModel
        {
            DisplayOrder = 1
        };
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateVendorModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var createVendorResult = await _mediator.Send(new CreateVendorCommand
        {
            Name = model.Name!,
            PhoneNumber = model.PhoneNumber!,
            Address = model.Address,
            DisplayOrder = model.DisplayOrder
        });
        if (!createVendorResult.Success)
        {
            ModelState.AddModelError(string.Empty, createVendorResult.ErrorMessage!);
            return View(model);
        }

        TempData[ViewConstants.UnitMeasurementSuccessMessage] = "Thêm mới nhà cung cấp thành công!";
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
        var resultDto = await _mediator.Send(new DeleteVendorCommand(id));
        if (!resultDto.Success)
            TempData[ViewConstants.VendorErrorMessage] = resultDto.ErrorMessage;

        TempData[ViewConstants.VendorSuccessMessage] = "Xóa nhà cung cấp thành công!";
        return RedirectToAction(nameof(List));
    }
}
