using MediatR;
using Microsoft.AspNetCore.Mvc;
using NamEcommerce.Web.Common;
using NamEcommerce.Web.Contracts.Commands.Models.Catalog;
using NamEcommerce.Web.Contracts.Queries.Models.Catalog;
using NamEcommerce.Web.Models.Catalog;

namespace NamEcommerce.Web.Controllers;

public sealed class CategoryController : BaseAuthorizedController
{
    private readonly AppConfig _appConfig;
    private readonly IMediator _mediator;

    public CategoryController(AppConfig appConfig, IMediator mediator)
    {
        _appConfig = appConfig;
        _mediator = mediator;
    }

    public IActionResult Index() => RedirectToAction(nameof(List));

    public IActionResult List(CategoryListSearchModel searchModel)
    {
        var pageNumber = searchModel?.PageNumber ?? 1;
        var pageSize = searchModel?.PageSize ?? 0;
        if (pageNumber <= 0) pageNumber = 1;
        if (pageSize <= 0) pageSize = _appConfig.DefaultPageSize;
        if (_appConfig.PageSizeOptions.Contains(pageSize)) pageSize = _appConfig.DefaultPageSize;

        var model = _mediator.Send(new GetCategoryListQuery
        {
            Keywords = searchModel?.Keywords,
            PageIndex = pageNumber - 1,
            PageSize = pageSize,
            BreadcrumbOpts = new()
            {
                ExcludeCurrent = true
            }
        }).Result;

        return View(model);
    }

    public async Task<IActionResult> Create()
    {
        var parentOptions = await _mediator.Send(new GetCategoryOptionListQuery());
        var model = new CreateCategoryModel
        {
            DisplayOrder = 1,
            Parents = parentOptions
        };

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateCategoryModel model)
    {
        if (!ModelState.IsValid)
        {
            var parentOptions = await _mediator.Send(new GetCategoryOptionListQuery());
            model.Parents = parentOptions;
            return View(model);
        }

        var createCategoryResult = await _mediator.Send(new CreateCategoryCommand
        {
            Name = model.Name!,
            ParentId = model.ParentId,
            DisplayOrder = model.DisplayOrder
        });
        if (!createCategoryResult.Success)
        {
            ModelState.AddModelError(string.Empty, createCategoryResult.ErrorMessage!);

            var parentOptions = await _mediator.Send(new GetCategoryOptionListQuery());
            model.Parents = parentOptions;
            return View(model);
        }

        TempData[ViewConstants.CategorySuccessMessage] = "Thêm mới danh mục thành công!";
        return RedirectToAction(nameof(List));
    }

    public async Task<IActionResult> Edit(Guid id)
    {
        var category = await _mediator.Send(new GetCategoryQuery { Id = id });
        if (category == null)
        {
            TempData[ViewConstants.CategoryErrorMessage] = "Không tìm thấy danh mục.";
            return RedirectToAction(nameof(List));
        }

        var parentOptions = await _mediator.Send(new GetCategoryOptionListQuery
        {
            ExcludedCategoryId = category.Id
        });
        var model = new EditCategoryModel
        {
            Id = category.Id,
            Name = category.Name,
            ParentId = category.ParentId,
            DisplayOrder = category.DisplayOrder,
            Parents = parentOptions
        };

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(EditCategoryModel model)
    {
        if (!ModelState.IsValid)
        {
            var parentOptions = await _mediator.Send(new GetCategoryOptionListQuery
            {
                ExcludedCategoryId = model.Id
            });
            model.Parents = parentOptions;
            return View(model);
        }

        var category = await _mediator.Send(new GetCategoryQuery { Id = model.Id });
        if (category == null)
        {
            TempData[ViewConstants.CategoryErrorMessage] = "Không tìm thấy danh mục.";
            return RedirectToAction(nameof(List));
        }

        var updateCategoryResult = await _mediator.Send(new UpdateCategoryCommand
        {
            Id = model.Id,
            Name = model.Name!,
            ParentId = model.ParentId,
            DisplayOrder = model.DisplayOrder
        });
        if (!updateCategoryResult.Success)
        {
            ModelState.AddModelError(string.Empty, updateCategoryResult.ErrorMessage!);

            var parentOptions = await _mediator.Send(new GetCategoryOptionListQuery
            {
                ExcludedCategoryId = category.Id
            });
            model.Parents = parentOptions;
            return View(model);
        }

        TempData[ViewConstants.CategorySuccessMessage] = "Chỉnh sửa danh mục thành công!";
        return RedirectToAction(nameof(List));
    }

    [HttpPost]
    public async Task<IActionResult> Delete(Guid id)
    {
        var resultDto = await _mediator.Send(new DeleteCategoryCommand(id));
        if (!resultDto.Success)
            TempData[ViewConstants.CategoryErrorMessage] = resultDto.ErrorMessage;

        TempData[ViewConstants.CategorySuccessMessage] = "Xóa danh mục thành công!";
        return RedirectToAction(nameof(List));
    }
}
