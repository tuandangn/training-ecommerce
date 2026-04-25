using MediatR;
using Microsoft.AspNetCore.Mvc;
using NamEcommerce.Web.Constants;
using NamEcommerce.Web.Contracts.Commands.Models.Catalog;
using NamEcommerce.Web.Contracts.Queries.Models.Catalog;
using NamEcommerce.Web.Models.Catalog;
using NamEcommerce.Web.Services.Catalog;

namespace NamEcommerce.Web.Controllers;

public sealed class CategoryController : BaseAuthorizedController
{
    private readonly IMediator _mediator;
    private readonly ICategoryModelFactory _categoryModelFactory;

    public CategoryController(IMediator mediator, ICategoryModelFactory categoryModelFactory)
    {
        _mediator = mediator;
        _categoryModelFactory = categoryModelFactory;
    }

    public IActionResult Index() => RedirectToAction(nameof(List));

    public async Task<IActionResult> List(CategoryListSearchModel searchModel)
    {
        var model = await _categoryModelFactory.PrepareCategoryListModel(searchModel);

        return View(model);
    }

    public async Task<IActionResult> Create()
    {
        var model = await _categoryModelFactory.PrepareCreateCategoryModel();

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateCategoryModel model)
    {
        if (!ModelState.IsValid)
        {
            model = await _categoryModelFactory.PrepareCreateCategoryModel(model);
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
            AddLocalizedModelError(createCategoryResult.ErrorMessage);

            model = await _categoryModelFactory.PrepareCreateCategoryModel(model);
            return View(model);
        }

        TempData[ViewConstants.CategorySuccessMessage] = LocalizeError("Msg.SaveSuccess");
        return RedirectToAction(nameof(List));
    }

    public async Task<IActionResult> Edit(Guid id)
    {
        var model = await _categoryModelFactory.PrepareEditCategoryModel(id);
        if (model == null)
        {
            TempData[ViewConstants.CategoryErrorMessage] = LocalizeError("Error.CategoryIsNotFound");
            return RedirectToAction(nameof(List));
        }

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(EditCategoryModel model)
    {
        if (!ModelState.IsValid)
        {
            model = (await _categoryModelFactory.PrepareEditCategoryModel(model.Id, model))!;
            return View(model);
        }

        var category = await _mediator.Send(new GetCategoryQuery { Id = model.Id });
        if (category == null)
        {
            TempData[ViewConstants.CategoryErrorMessage] = LocalizeError("Error.CategoryIsNotFound");
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
            AddLocalizedModelError(updateCategoryResult.ErrorMessage);

            model = (await _categoryModelFactory.PrepareEditCategoryModel(model.Id, model))!;
            return View(model);
        }

        TempData[ViewConstants.CategorySuccessMessage] = LocalizeError("Msg.SaveSuccess");
        return RedirectToAction(nameof(List));
    }

    [HttpGet]
    public async Task<IActionResult> Options()
    {
        var options = await _mediator.Send(new GetCategoryOptionListQuery());
        var result = options.Options.Select(o => new { id = o.Id, name = o.Name }).ToList();
        return Json(result);
    }

    [HttpPost]
    public async Task<IActionResult> Delete(Guid id)
    {
        var resultDto = await _mediator.Send(new DeleteCategoryCommand(id));
        if (!resultDto.Success)
            TempData[ViewConstants.CategoryErrorMessage] = LocalizeError(resultDto.ErrorMessage!);
        else
            TempData[ViewConstants.CategorySuccessMessage] = LocalizeError("Msg.DeleteSuccess");
        return RedirectToAction(nameof(List));
    }
}
