using MediatR;
using Microsoft.AspNetCore.Mvc;
using NamEcommerce.Web.Constants;
using NamEcommerce.Web.Contracts.Commands.Models.Catalog;
using NamEcommerce.Web.Contracts.Configurations;
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

        TempData[ViewConstants.VendorSuccessMessage] = "Thêm mới nhà cung cấp thành công!";
        return RedirectToAction(nameof(List));
    }

    public async Task<IActionResult> Edit(Guid id)
    {
        var vendor = await _mediator.Send(new GetVendorQuery { Id = id });
        if (vendor == null)
        {
            TempData[ViewConstants.VendorErrorMessage] = "Không tìm thấy nhà cung cấp.";
            return RedirectToAction(nameof(List));
        }

        var model = new EditVendorModel
        {
            Id = vendor.Id,
            Name = vendor.Name,
            PhoneNumber = vendor.PhoneNumber,
            Address = vendor.Address,
            DisplayOrder = vendor.DisplayOrder
        };

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(EditVendorModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var vendor = await _mediator.Send(new GetVendorQuery { Id = model.Id });
        if (vendor == null)
        {
            TempData[ViewConstants.VendorErrorMessage] = "Không tìm thấy nhà cung cấp.";
            return RedirectToAction(nameof(List));
        }

        var updateVendorResult = await _mediator.Send(new UpdateVendorCommand
        {
            Id = model.Id,
            Name = model.Name,
            PhoneNumber = model.PhoneNumber,
            Address = model.Address,
            DisplayOrder = model.DisplayOrder
        });
        if (!updateVendorResult.Success)
        {
            ModelState.AddModelError(string.Empty, updateVendorResult.ErrorMessage!);
            return View(model);
        }

        TempData[ViewConstants.VendorSuccessMessage] = "Chỉnh sửa nhà cung cấp thành công!";
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

    [HttpGet]
    public async Task<IActionResult> Search(string q)
    {
        var model = await _mediator.Send(new GetVendorListQuery
        {
            Keywords = q,
            PageIndex = 0,
            PageSize = int.MaxValue
        });

        var vendors = model.Data.Items.Select(vendor => new
        {
            id = vendor.Id,
            name = vendor.Name,
            phone = vendor.PhoneNumber,
            address = vendor.Address
        }).ToList();
        return Json(vendors);
    }
}
