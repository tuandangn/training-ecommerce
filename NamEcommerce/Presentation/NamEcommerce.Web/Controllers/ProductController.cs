using MediatR;
using Microsoft.AspNetCore.Mvc;
using NamEcommerce.Web.Common;
using NamEcommerce.Web.Contracts.Commands.Models.Catalog;
using NamEcommerce.Web.Contracts.Models.Common;
using NamEcommerce.Web.Contracts.Queries.Models.Catalog;
using NamEcommerce.Web.Models.Catalog;

namespace NamEcommerce.Web.Controllers;

public sealed class ProductController : BaseAuthorizedController
{
    private readonly AppConfig _appConfig;
    private readonly IMediator _mediator;

    public ProductController(AppConfig appConfig, IMediator mediator)
    {
        _appConfig = appConfig;
        _mediator = mediator;
    }

    public IActionResult Index() => RedirectToAction(nameof(List));

    public IActionResult List(ProductListSearchModel searchModel)
    {
        var pageNumber = searchModel?.PageNumber ?? 1;
        var pageSize = searchModel?.PageSize ?? 0;
        if (pageNumber <= 0) pageNumber = 1;
        if (pageSize <= 0) pageSize = _appConfig.DefaultPageSize;
        if (_appConfig.PageSizeOptions.Contains(pageSize)) pageSize = _appConfig.DefaultPageSize;

        var model = _mediator.Send(new GetProductListQuery
        {
            Keywords = searchModel?.Keywords,
            PageIndex = pageNumber - 1,
            PageSize = pageSize
        }).Result;

        return View(model);
    }

    public async Task<IActionResult> Create()
    {
        var categoryOptions = await _mediator.Send(new GetCategoryOptionListQuery());
        var model = new CreateProductModel
        {
            Categories = categoryOptions,
            DisplayOrder = 1
        };
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateProductModel model)
    {
        if (!ModelState.IsValid)
            goto ReturnToView;

        FileInfoModel? imageFileInfo = null;
        if (model.ImageFile != null)
        {
            var imageBinaries = model.ImageFile.GetData();
            var imageMimeType = model.ImageFile.GetMimeType();
            if (imageBinaries is not null && imageBinaries.Length > 0 && !string.IsNullOrEmpty(imageMimeType))
            {
                if (imageBinaries.Length > _appConfig.UploadFileMaxSizeInBytes)
                {
                    ModelState.AddModelError(string.Empty, $"Kích thước hình ảnh phải nhỏ hơn {(int)Math.Floor(_appConfig.UploadFileMaxSizeInBytes / 1024m / 1024)}Mb.");
                    goto ReturnToView;
                }

                imageFileInfo = new FileInfoModel
                {
                    Data = imageBinaries,
                    MimeType = imageMimeType,
                    Extension = model.ImageFile.Extension,
                    FileName = model.ImageFile.FileName
                };
            }
        }
        var createProductResult = await _mediator.Send(new CreateProductCommand
        {
            Name = model.Name!,
            ShortDesc = model.ShortDesc,
            CategoryId = model.CategoryId,
            DisplayOrder = model.DisplayOrder,
            ImageFile = imageFileInfo
        });
        if (!createProductResult.Success)
        {
            ModelState.AddModelError(string.Empty, createProductResult.ErrorMessage!);
            goto ReturnToView;
        }

        TempData[ViewConstants.ProductSuccessMessage] = "Thêm mới hàng hóa thành công!";
        return RedirectToAction(nameof(List));

    ReturnToView:
        var categoryOptions = await _mediator.Send(new GetCategoryOptionListQuery());
        model.Categories = categoryOptions;
        return View(model);

    }

    public async Task<IActionResult> Edit(Guid id)
    {
        var product = await _mediator.Send(new GetProductQuery { Id = id });
        if (product == null)
        {
            TempData[ViewConstants.ProductErrorMessage] = "Không tìm thấy hàng hóa.";
            return RedirectToAction(nameof(List));
        }

        var categoryOptions = await _mediator.Send(new GetCategoryOptionListQuery());
        var model = new EditProductModel
        {
            Id = product.Id,
            Name = product.Name,
            ShortDesc = product.ShortDesc,
            Categories = categoryOptions,
            CategoryId = product.CategoryId,
            DisplayOrder = product.DisplayOrder,
            ImageFile = product.ImageFile ?? new()
        };

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(EditProductModel model)
    {
        if (!ModelState.IsValid)
            goto ReturnToView;

        var product = await _mediator.Send(new GetProductQuery { Id = model.Id });
        if (product == null)
        {
            TempData[ViewConstants.ProductErrorMessage] = "Không tìm thấy hàng hóa.";
            return RedirectToAction(nameof(List));
        }

        FileInfoModel? imageFileInfo = null;
        if (model.ImageFile != null)
        {
            var imageBinaries = model.ImageFile.GetData();
            var imageMimeType = model.ImageFile.GetMimeType();
            if (imageBinaries is not null && imageBinaries.Length > 0 && !string.IsNullOrEmpty(imageMimeType))
            {
                if (imageBinaries.Length > _appConfig.UploadFileMaxSizeInBytes)
                {
                    ModelState.AddModelError(string.Empty, $"Kích thước hình ảnh phải nhỏ hơn {(int)Math.Floor(_appConfig.UploadFileMaxSizeInBytes / 1024m / 1024)}Mb.");
                    goto ReturnToView;
                }

                imageFileInfo = new FileInfoModel
                {
                    Data = imageBinaries,
                    MimeType = imageMimeType,
                    Extension = model.ImageFile.Extension,
                    FileName = model.ImageFile.FileName
                };
            }
        }
        var updateProductResult = await _mediator.Send(new UpdateProductCommand
        {
            Id = model.Id,
            Name = model.Name,
            ShortDesc = model.ShortDesc,
            CategoryId = model.CategoryId,
            DisplayOrder = model.DisplayOrder,
            ImageFile = imageFileInfo
        });
        if (!updateProductResult.Success)
        {
            ModelState.AddModelError(string.Empty, updateProductResult.ErrorMessage!);
            goto ReturnToView;
        }

        TempData[ViewConstants.ProductSuccessMessage] = "Chỉnh sửa hàng hóa thành công!";
        return RedirectToAction(nameof(List));

    ReturnToView:
        var categoryOptions = await _mediator.Send(new GetCategoryOptionListQuery());
        model.Categories = categoryOptions;
        return View(model);

    }

    [HttpPost]
    public async Task<IActionResult> Delete(Guid id)
    {
        var resultDto = await _mediator.Send(new DeleteProductCommand(id));
        if (!resultDto.Success)
            TempData[ViewConstants.ProductErrorMessage] = resultDto.ErrorMessage;

        TempData[ViewConstants.ProductSuccessMessage] = "Xóa hàng hóa thành công!";
        return RedirectToAction(nameof(List));
    }
}
