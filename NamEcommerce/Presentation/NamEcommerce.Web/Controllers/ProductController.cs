using MediatR;
using Microsoft.AspNetCore.Mvc;
using NamEcommerce.Web.Constants;
using NamEcommerce.Web.Contracts.Commands.Models.Catalog;
using NamEcommerce.Web.Contracts.Configurations;
using NamEcommerce.Web.Contracts.Models.Common;
using NamEcommerce.Web.Contracts.Queries.Models.Catalog;
using NamEcommerce.Web.Models.Catalog;
using NamEcommerce.Web.Services.Catalog;

namespace NamEcommerce.Web.Controllers;

public sealed class ProductController : BaseAuthorizedController
{
    private readonly AppConfig _appConfig;
    private readonly IMediator _mediator;
    private readonly IProductModelFactory _productModelFactory;

    public ProductController(AppConfig appConfig, IMediator mediator, IProductModelFactory productModelFactory)
    {
        _appConfig = appConfig;
        _mediator = mediator;
        _productModelFactory = productModelFactory;
    }

    public IActionResult Index() => RedirectToAction(nameof(List));

    public async Task<IActionResult> List(ProductListSearchModel searchModel)
    {
        var model = await _productModelFactory.PrepareProductListModel(searchModel);

        return View(model);
    }

    public async Task<IActionResult> Create()
    {
        var model = await _productModelFactory.PrepareCreateProductModel();
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateProductModel model)
    {
        if (!ModelState.IsValid)
        {
            model = await _productModelFactory.PrepareCreateProductModel(model);
            return View(model);
        }

        var imageFileInfo = model.ImageFile != null ? new FileInfoModel
        {
            Data = model.ImageFile.GetData() ?? [],
            MimeType = model.ImageFile.GetMimeType() ?? string.Empty,
            Extension = model.ImageFile.Extension,
            FileName = model.ImageFile.FileName
        } : null;
        if (imageFileInfo is not null && imageFileInfo.Data.Length > _appConfig.UploadFileMaxSizeInBytes)
        {
            ModelState.AddModelError(string.Empty, LocalizeError("Msg.ImageSizeLimit", (int)Math.Floor(_appConfig.UploadFileMaxSizeInBytes / 1024m / 1024)));
            model = await _productModelFactory.PrepareCreateProductModel(model);
            return View(model);
        }

        var createProductResult = await _mediator.Send(new CreateProductCommand
        {
            Name = model.Name!,
            ShortDesc = model.ShortDesc,
            CategoryId = model.CategoryId,
            VendorIds = model.VendorIds,
            UnitMeasurementId = model.UnitMeasurementId,
            UnitPrice = model.UnitPrice,
            CostPrice = model.CostPrice,
            DisplayOrder = model.DisplayOrder,
            ImageFile = imageFileInfo
        });
        if (!createProductResult.Success)
        {
            AddLocalizedModelError(createProductResult.ErrorMessage);
            model = await _productModelFactory.PrepareCreateProductModel(model);
            return View(model);
        }

        TempData[ViewConstants.ProductSuccessMessage] = LocalizeError("Msg.SaveSuccess");
        return RedirectToAction(nameof(List));
    }

    public async Task<IActionResult> Edit(Guid id)
    {
        var model = await _productModelFactory.PrepareEditProductModel(id);
        if (model == null)
        {
            TempData[ViewConstants.ProductErrorMessage] = LocalizeError("Error.ProductIsNotFound");
            return RedirectToAction(nameof(List));
        }

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(EditProductModel model)
    {
        if (!ModelState.IsValid)
        {
            model = (await _productModelFactory.PrepareEditProductModel(model.Id, model))!;
            return View(model);
        }

        var product = await _mediator.Send(new GetProductByIdQuery { Id = model.Id });
        if (product == null)
        {
            TempData[ViewConstants.ProductErrorMessage] = LocalizeError("Error.ProductIsNotFound");
            return RedirectToAction(nameof(List));
        }

        var imageFileInfo = model.ImageFile != null ? new FileInfoModel
        {
            Data = model.ImageFile.GetData() ?? [],
            MimeType = model.ImageFile.GetMimeType() ?? string.Empty,
            Extension = model.ImageFile.Extension,
            FileName = model.ImageFile.FileName
        } : null;
        if (imageFileInfo is not null && imageFileInfo.Data.Length > _appConfig.UploadFileMaxSizeInBytes)
        {
            ModelState.AddModelError(string.Empty, LocalizeError("Msg.ImageSizeLimit", (int)Math.Floor(_appConfig.UploadFileMaxSizeInBytes / 1024m / 1024)));
            model = (await _productModelFactory.PrepareEditProductModel(model.Id, model))!;
            return View(model);
        }

        var updateProductResult = await _mediator.Send(new UpdateProductCommand
        {
            Id = model.Id,
            Name = model.Name,
            ShortDesc = model.ShortDesc,
            CategoryId = model.CategoryId,
            VendorIds = model.VendorIds,
            UnitMeasurementId = model.UnitMeasurementId,
            UnitPrice = model.UnitPrice,
            CostPrice = model.CostPrice,
            DisplayOrder = model.DisplayOrder,
            ImageFile = imageFileInfo,
            ChangePriceReason = model.ChangePriceReason
        });
        if (!updateProductResult.Success)
        {
            AddLocalizedModelError(updateProductResult.ErrorMessage);
            model = (await _productModelFactory.PrepareEditProductModel(model.Id, model))!;
            return View(model);
        }

        TempData[ViewConstants.ProductSuccessMessage] = LocalizeError("Msg.SaveSuccess");
        return RedirectToAction(nameof(List));
    }

    [HttpGet]
    public async Task<IActionResult> Search(string q, Guid? w, Guid? vendorId, Guid? categoryId)
    {
        var model = await _mediator.Send(new GetProductListForOrderQuery
        {
            Keywords = q,
            VendorId = vendorId,
            WarehouseId = w,
            CategoryId = categoryId
        });

        var products = model.Data.Items.Select(productInfo => new
        {
            id = productInfo.Id,
            name = productInfo.Name,
            picture = productInfo.PictureUrl,
            availableQty = productInfo.QuantityAvailable,
            categoryName = productInfo.CategoryName,
            avaialbeWarehouses = productInfo.AvailableWarehouseIds,
            vendorCount = productInfo.AvailableVendors.Count,
            firstVendorId = productInfo.AvailableVendors.FirstOrDefault()?.Id.ToString(),
            availableVendors = productInfo.AvailableVendors.Select(v => new { key = v.Id.ToString(), value = v.Name })
        }).ToList();
        return Json(products);
    }

    [HttpPost]
    public async Task<IActionResult> Delete(Guid id)
    {
        var resultDto = await _mediator.Send(new DeleteProductCommand(id));
        if (!resultDto.Success)
            TempData[ViewConstants.ProductErrorMessage] = LocalizeError(resultDto.ErrorMessage!);
        else
            TempData[ViewConstants.ProductSuccessMessage] = LocalizeError("Msg.DeleteSuccess");
        return RedirectToAction(nameof(List));
    }

    [HttpGet]
    public async Task<IActionResult> PriceHistory(GetProductPriceHistoryQuery query)
    {
        var model = await _mediator.Send(query).ConfigureAwait(false);
        return Json(model);
    }
}
