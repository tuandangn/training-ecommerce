using MediatR;
using Microsoft.AspNetCore.Mvc;
using NamEcommerce.Web.Contracts.Commands.Models.Preparation;
using NamEcommerce.Web.Contracts.Commands.Models.PurchaseOrders;
using NamEcommerce.Web.Contracts.Queries.Models.Catalog;
using NamEcommerce.Web.Models.Preparations;
using NamEcommerce.Web.Services.Preparations;

namespace NamEcommerce.Web.Controllers;

public sealed class PreparationController : BaseAuthorizedController
{
    private readonly IMediator _mediator;
    private readonly IPreparationModelFactory _preparationModelFactory;

    public PreparationController(IMediator mediator, IPreparationModelFactory preparationModelFactory)
    {
        _mediator = mediator;
        _preparationModelFactory = preparationModelFactory;
    }

    public async Task<IActionResult> List(PreparationListSearchModel searchModel, bool grouped = false)
    {
        object model = grouped ? await _preparationModelFactory.PrepareProductPreparationListModelAsync(searchModel)
                : await _preparationModelFactory.PrepareCustomerPreparationListModelAsync(searchModel);

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> GetProductVendors(Guid productId)
    {
        var product = await _mediator.Send(new GetProductByIdQuery { Id = productId });
        if (product is null || product.VendorIds.Count == 0)
            return Json(new { vendors = Array.Empty<object>() });

        var allVendors = await _mediator.Send(new GetVendorOptionListQuery());
        var productVendors = allVendors.Options
            .Where(v => product.VendorIds.Contains(v.Id))
            .Select(v => new { id = v.Id, name = v.Name });

        return Json(new { vendors = productVendors });
    }

    [HttpPost]
    public async Task<IActionResult> QuickCreatePurchaseOrder([FromBody] QuickCreatePurchaseOrderModel model)
    {
        var result = await _mediator.Send(new CreatePurchaseOrderCommand
        {
            VendorId = model.VendorId,
            Note = model.Note,
            Items = [new CreatePurchaseOrderItemCommand
            {
                ProductId = model.ProductId,
                Quantity = model.Quantity,
                UnitCost = model.UnitCost
            }]
        });

        if (!result.Success)
            return Json(new { success = false, message = result.ErrorMessage });

        return Json(new { success = true, message = "Đã tạo đơn nhập hàng thành công.", purchaseOrderId = result.CreatedId });
    }

    [HttpPost]
    public async Task<IActionResult> MarkDelivered(Guid orderId, Guid orderItemId, IFormFile? proofImage)
    {
        if (proofImage is null || proofImage.Length == 0)
            return Json(new { success = false, message = "Hình ảnh bằng chứng giao hàng là bắt buộc." });

        byte[] pictureData;
        using (var memoryStream = new MemoryStream())
        {
            await proofImage.CopyToAsync(memoryStream);
            pictureData = memoryStream.ToArray();
        }

        var result = await _mediator.Send(new MarkOrderItemDeliveredCommand(
            orderId, 
            orderItemId, 
            pictureData, 
            proofImage.FileName, 
            proofImage.ContentType));

        if (!result.Success)
            return Json(new { success = false, message = result.ErrorMessage });

        return Json(new { success = true, message = "Đã đánh dấu giao hàng thành công." });
    }
}
