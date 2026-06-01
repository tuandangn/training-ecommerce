using MediatR;
using Microsoft.AspNetCore.Mvc;
using NamEcommerce.Web.Contracts.Commands.Models.Catalog;
using NamEcommerce.Web.Contracts.Queries.Models.Catalog;
using NamEcommerce.Web.Models.Catalog;
using NamEcommerce.Web.Services.Catalog;

namespace NamEcommerce.Web.Controllers;

public sealed class ProductController : BaseAuthorizedController
{
    private readonly IMediator _mediator;
    private readonly IProductModelFactory _productModelFactory;

    public ProductController(IMediator mediator, IProductModelFactory productModelFactory)
    {
        _mediator = mediator;
        _productModelFactory = productModelFactory;
    }

    public IActionResult Index() => RedirectToAction(nameof(List));

    public async Task<IActionResult> List(ProductSearchModel searchModel)
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

        var createProductCommand = new CreateProductCommand
        {
            Name = model.Name!,
            ShortDesc = model.ShortDesc,
            CategoryId = model.CategoryId,
            VendorIds = model.VendorIds,
            UnitMeasurementId = model.UnitMeasurementId,
            DisplayOrder = model.DisplayOrder,
            PictureId = model.PictureId,
            UnitPrice = model.UnitPrice
        };
        if (model.HasExistingStockQuantity)
        {
            createProductCommand.ProductStocks = model.ProductInventory!.ProductStocks
                .Where(stock => stock.WarehouseId != Guid.Empty && stock.Quantity > 0 && stock.UnitCost > 0)
                .Select(stock => new CreateProductCommand.ProductStockModel(stock.WarehouseId, stock.Quantity, stock.UnitCost));
        }

        var createProductResult = await _mediator.Send(createProductCommand);
        if (!createProductResult.Success)
        {
            AddLocalizedModelError(createProductResult.ErrorMessage);
            model = await _productModelFactory.PrepareCreateProductModel(model);
            return View(model);
        }

        NotifySuccess("Msg.SaveSuccess");
        return RedirectToAction(nameof(List));
    }

    [HttpPost]
    public async Task<IActionResult> QuickCreate(CreateProductModel model)
    {
        if (model.VendorIds.Count == 0)
            ModelState.AddModelError(nameof(model.VendorIds), "Vui lòng chọn nhà cung cấp để thêm hàng hóa vào đơn.");

        if (!ModelState.IsValid)
            return Json(new { success = false, message = GetErrorMessage() });

        var createProductResult = await _mediator.Send(new CreateProductCommand
        {
            Name = model.Name!,
            ShortDesc = model.ShortDesc,
            CategoryId = model.CategoryId,
            VendorIds = model.VendorIds,
            UnitMeasurementId = model.UnitMeasurementId,
            DisplayOrder = model.DisplayOrder <= 0 ? 1 : model.DisplayOrder,
            UnitPrice = model.UnitPrice
        });

        if (!createProductResult.Success)
            return Json(new { success = false, message = LocalizeError(createProductResult.ErrorMessage!) });

        var createdProducts = await _mediator.Send(new GetProductsByIdsForOrderQuery
        {
            Ids = [createProductResult.CreatedId]
        });
        var product = createdProducts.FirstOrDefault();
        if (product is null)
            return Json(new { success = false, message = LocalizeError("Error.ProductIsNotFound") });

        return Json(new
        {
            success = true,
            message = LocalizeError("Msg.SaveSuccess"),
            product = new
            {
                id = product.Id,
                name = product.Name,
                unitMeasurement = product.UnitMeasurement,
                picture = product.PictureUrl,
                unitPrice = product.CurrentUnitPrice,
                availableQty = product.QuantityAvailable,
                categoryName = product.CategoryName,
                availableWarehouses = product.AvailableWarehouses,
                vendorCount = product.AvailableVendors.Count,
                firstVendorId = product.AvailableVendors.FirstOrDefault()?.Id.ToString(),
                availableVendors = product.AvailableVendors.Select(v => new { key = v.Id.ToString(), value = v.Name })
            },
            unitPrice = model.UnitPrice
        });
    }

    public async Task<IActionResult> Edit(Guid id)
    {
        var model = await _productModelFactory.PrepareEditProductModel(id);
        if (model == null)
        {
            NotifyError("Error.ProductIsNotFound");
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
            NotifyError("Error.ProductIsNotFound");
            return RedirectToAction(nameof(List));
        }

        var updateProductResult = await _mediator.Send(new UpdateProductCommand
        {
            Id = model.Id,
            Name = model.Name,
            ShortDesc = model.ShortDesc,
            CategoryId = model.CategoryId,
            VendorIds = model.VendorIds,
            UnitMeasurementId = model.UnitMeasurementId,
            NewUnitPrice = model.UnitPrice,
            DisplayOrder = model.DisplayOrder,
            PictureId = model.PictureId,
            ChangePriceReason = model.ChangePriceReason
        });
        if (!updateProductResult.Success)
        {
            AddLocalizedModelError(updateProductResult.ErrorMessage);
            model = (await _productModelFactory.PrepareEditProductModel(model.Id, model))!;
            return View(model);
        }

        NotifySuccess("Msg.SaveSuccess");
        return RedirectToAction(nameof(List));
    }

    [HttpGet]
    public async Task<IActionResult> Search(ProductSearchModel searchModel, Guid? wid)
    {
        var model = await _mediator.Send(new GetProductListForOrderQuery
        {
            Keywords = searchModel.Q,
            VendorId = searchModel.Vid,
            WarehouseId = wid,
            CategoryId = searchModel.Cid
        });

        var products = model.Data.Items.Select(productInfo => new
        {
            id = productInfo.Id,
            name = productInfo.Name,
            unitMeasurement = productInfo.UnitMeasurement,
            picture = productInfo.PictureUrl,
            unitPrice = productInfo.UnitPrice,
            availableQty = productInfo.QuantityAvailable,
            categoryName = productInfo.CategoryName,
            availableWarehouses = productInfo.AvailableWarehouses,
            vendorCount = productInfo.AvailableVendors.Count(),
            firstVendorId = productInfo.AvailableVendors.FirstOrDefault()?.Id.ToString(),
            availableVendors = productInfo.AvailableVendors.Select(v => new { key = v.Id.ToString(), value = v.Name })
        }).ToList();
        return Json(products);
    }

    [HttpGet]
    public async Task<IActionResult> PickItem(Guid id)
    {
        var model = await _mediator.Send(new GetProductsByIdsForOrderQuery
        {
            Ids = [id]
        });
        var product = model.FirstOrDefault();
        if (product is null)
            return NotFound();

        return Json(new
        {
            id = product.Id,
            name = product.Name,
            unitMeasurement = product.UnitMeasurement,
            picture = product.PictureUrl,
            unitPrice = product.CurrentUnitPrice,
            availableQty = product.QuantityAvailable,
            categoryName = product.CategoryName,
            availableWarehouses = product.AvailableWarehouses,
            vendorCount = product.AvailableVendors.Count,
            firstVendorId = product.AvailableVendors.FirstOrDefault()?.Id.ToString(),
            availableVendors = product.AvailableVendors.Select(v => new { key = v.Id.ToString(), value = v.Name })
        });
    }

    [HttpPost]
    public async Task<IActionResult> Delete(Guid id)
    {
        var resultDto = await _mediator.Send(new DeleteProductCommand(id));
        if (!resultDto.Success)
            NotifyError(resultDto.ErrorMessage!);
        else
            NotifySuccess("Msg.DeleteSuccess");
        return RedirectToAction(nameof(List));
    }

    [HttpGet]
    public async Task<IActionResult> PriceHistory(GetProductPriceHistoryQuery query)
    {
        var model = await _mediator.Send(query).ConfigureAwait(false);
        return Json(model);
    }

    [HttpGet]
    public async Task<IActionResult> SalePriceReference(GetProductSalePriceReferenceQuery query)
    {
        var model = await _mediator.Send(query).ConfigureAwait(false);
        return Json(model);
    }

    [HttpGet]
    public async Task<IActionResult> PurchasePriceReference(GetProductPurchasePriceReferenceQuery query)
    {
        var model = await _mediator.Send(query).ConfigureAwait(false);
        return Json(model);
    }
}
