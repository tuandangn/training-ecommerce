using Microsoft.AspNetCore.Mvc;
using NamEcommerce.Application.Contracts.CustomerPortal;
using NamEcommerce.Application.Contracts.Dtos.CustomerPortal;
using NamEcommerce.Application.Contracts.Inventory;
using NamEcommerce.Data.Contracts;

namespace NamEcommerce.Web.Controllers;

public sealed class CustomerPortalController(
    ICustomerPortalAdminAppService customerPortalAdminAppService,
    IWarehouseAppService warehouseAppService, IUnitOfWork unitOfWork) : BaseAuthorizedController
{
    public async Task<IActionResult> Index()
    {
        var model = await customerPortalAdminAppService.GetOverviewAsync();
        return View(model);
    }

    public async Task<IActionResult> Accounts()
    {
        var model = await customerPortalAdminAppService.GetAccountsAsync();
        return View(model);
    }

    public async Task<IActionResult> AccountDetails(Guid customerId)
    {
        var model = await customerPortalAdminAppService.GetAccountAsync(customerId);
        if (model is null)
        {
            NotifyError("Không tìm thấy tài khoản.");
            return RedirectToAction(nameof(Accounts));
        }

        var securityEvents = await customerPortalAdminAppService.GetSecurityEventsAsync(customerId);
        ViewBag.SecurityEvents = securityEvents;

        return View(model);
    }

    public async Task<IActionResult> SecurityEvents(Guid? customerId = null)
    {
        var model = await customerPortalAdminAppService.GetSecurityEventsAsync(customerId);
        return View(model);
    }

    public IActionResult Notifications()
        => RedirectToAction("List", "SystemNotification",
            new { TypeGroup = (int)Web.Contracts.Models.Notifications.SystemNotificationTypeGroup.CustomerPortal });

    public async Task<IActionResult> Settings()
    {
        var model = await customerPortalAdminAppService.GetSettingsAsync();
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> UpdateSettings(bool otpEnabled)
    {
        var result = await customerPortalAdminAppService
            .UpdateSettingsAsync(new UpdateCustomerPortalSettingsAdminAppDto { OtpEnabled = otpEnabled });
        await unitOfWork.CommitAsync();

        NotifyResult(result);
        return RedirectToAction(nameof(Settings));
    }

    public async Task<IActionResult> OrderRequests(int? status = null)
    {
        var model = await customerPortalAdminAppService.GetOrderRequestsAsync(status);
        ViewData["Status"] = status;
        return View(model);
    }

    public async Task<IActionResult> OrderRequestDetails(Guid id)
    {
        var model = await customerPortalAdminAppService.GetOrderRequestAsync(id);
        if (model is null)
        {
            NotifyError("Không tìm thấy yêu cầu đặt hàng.");
            return RedirectToAction(nameof(OrderRequests));
        }

        return View(model);
    }

    public async Task<IActionResult> ReturnRequests(int? status = null)
    {
        var model = await customerPortalAdminAppService.GetReturnRequestsAsync(status);
        ViewData["Status"] = status;
        return View(model);
    }

    public async Task<IActionResult> ReturnRequestDetails(Guid id)
    {
        var model = await customerPortalAdminAppService.GetReturnRequestAsync(id);
        if (model is null)
        {
            NotifyError("Không tìm thấy yêu cầu trả hàng.");
            return RedirectToAction(nameof(ReturnRequests));
        }

        await PrepareWarehouseOptionsAsync();
        return View(model);
    }

    public async Task<IActionResult> PaymentIntents(int? status = null)
    {
        var model = await customerPortalAdminAppService.GetPaymentIntentsAsync(status);
        ViewData["Status"] = status;
        return View(model);
    }

    public async Task<IActionResult> PaymentIntentDetails(Guid id)
    {
        var model = await customerPortalAdminAppService.GetPaymentIntentAsync(id);
        if (model is null)
        {
            NotifyError("Không tìm thấy thanh toán online.");
            return RedirectToAction(nameof(PaymentIntents));
        }

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> BlockAccount(Guid customerId, string? returnUrl = null)
    {
        var result = await customerPortalAdminAppService.BlockAccountAsync(customerId);
        await unitOfWork.CommitAsync();

        NotifyResult(result);
        return RedirectBack(returnUrl, nameof(Accounts));
    }

    [HttpPost]
    public async Task<IActionResult> UnblockAccount(Guid customerId, string? returnUrl = null)
    {
        var result = await customerPortalAdminAppService.UnblockAccountAsync(customerId);
        await unitOfWork.CommitAsync();

        NotifyResult(result);
        return RedirectBack(returnUrl, nameof(Accounts));
    }

    [HttpPost]
    public async Task<IActionResult> ResetAccountPassword(Guid customerId, string password, string? returnUrl = null)
    {
        var result = await customerPortalAdminAppService.ResetAccountPasswordAsync(customerId, password);
        await unitOfWork.CommitAsync();

        NotifyResult(result);
        return RedirectBack(returnUrl, nameof(Accounts));
    }

    [HttpPost]
    public async Task<IActionResult> ApproveOrderRequest(Guid id, Guid[] itemIds, decimal[] unitPrices, string? adminNote)
    {
        var itemPrices = itemIds
            .Zip(unitPrices, (itemId, unitPrice) => new { itemId, unitPrice })
            .GroupBy(item => item.itemId)
            .ToDictionary(group => group.Key, group => group.Last().unitPrice);
        var result = await customerPortalAdminAppService.ApproveOrderRequestAsync(id, itemPrices, adminNote);
        await unitOfWork.CommitAsync();

        NotifyResult(result);
        return RedirectToAction(nameof(OrderRequestDetails), new { id });
    }

    [HttpPost]
    public async Task<IActionResult> RejectOrderRequest(Guid id, string? adminNote)
    {
        var result = await customerPortalAdminAppService.RejectOrderRequestAsync(id, adminNote);
        await unitOfWork.CommitAsync();

        NotifyResult(result);
        return RedirectToAction(nameof(OrderRequestDetails), new { id });
    }

    [HttpPost]
    public async Task<IActionResult> ConvertOrderRequest(Guid id)
    {
        var result = await customerPortalAdminAppService.ConvertOrderRequestAsync(id);
        await unitOfWork.CommitAsync();

        NotifyResult(result);

        return result.Success && result.CreatedId.HasValue
            ? RedirectToAction("Details", "Order", new { id = result.CreatedId.Value })
            : RedirectToAction(nameof(OrderRequestDetails), new { id });
    }

    [HttpPost]
    public async Task<IActionResult> AcceptReturnRequest(Guid id, string? adminNote)
    {
        var result = await customerPortalAdminAppService.AcceptReturnRequestAsync(id, adminNote);
        await unitOfWork.CommitAsync();

        NotifyResult(result);
        return RedirectToAction(nameof(ReturnRequestDetails), new { id });
    }

    [HttpPost]
    public async Task<IActionResult> RejectReturnRequest(Guid id, string? adminNote)
    {
        var result = await customerPortalAdminAppService.RejectReturnRequestAsync(id, adminNote);
        await unitOfWork.CommitAsync();

        NotifyResult(result);
        return RedirectToAction(nameof(ReturnRequestDetails), new { id });
    }

    [HttpPost]
    public async Task<IActionResult> ConvertReturnRequest(
        Guid id, Guid warehouseId, Guid[] itemIds,
        decimal[] acceptedQuantities, decimal[] returnUnitPrices,
        decimal additionalCost, string? adminNote)
    {
        var items = itemIds
            .Select((itemId, index) => new CustomerPortalReturnConversionItemAppDto
            {
                RequestItemId = itemId,
                AcceptedQuantity = index < acceptedQuantities.Length ? acceptedQuantities[index] : 0,
                ReturnUnitPrice = index < returnUnitPrices.Length ? returnUnitPrices[index] : 0
            })
            .ToList();

        var result = await customerPortalAdminAppService
            .ConvertReturnRequestAsync(id, warehouseId, items, additionalCost, adminNote);
        await unitOfWork.CommitAsync();

        NotifyResult(result);
        return result.Success && result.CreatedId.HasValue
            ? RedirectToAction("Details", "CustomerReturn", new { id = result.CreatedId.Value })
            : RedirectToAction(nameof(ReturnRequestDetails), new { id });
    }

    [HttpPost]
    public async Task<IActionResult> ReconcilePaymentIntent(Guid id, string? adminNote)
    {
        var result = await customerPortalAdminAppService.ReconcilePaymentIntentAsync(id, adminNote);
        await unitOfWork.CommitAsync();

        NotifyResult(result);
        return RedirectToAction(nameof(PaymentIntentDetails), new { id });
    }

    private void NotifyResult(CustomerActionResultAppDto result)
    {
        if (result.Success)
            NotificationService.Success(ResolveNotificationMessage(result.Message, "Thao tác thành công."));
        else
            NotificationService.Error(ResolveNotificationMessage(result.Message, "Thao tác không thành công."));
    }

    private void NotifyResult(CustomerPortalConversionResultAppDto result)
    {
        if (result.Success)
            NotificationService.Success(ResolveNotificationMessage(result.Message, "Chuyển đổi thành công."));
        else
            NotificationService.Error(ResolveNotificationMessage(result.Message, "Chuyển đổi không thành công."));
    }

    private string ResolveNotificationMessage(string? message, string fallback)
    {
        if (string.IsNullOrWhiteSpace(message))
            return fallback;

        return message.StartsWith("Error.", StringComparison.Ordinal)
            ? LocalizeError(message)
            : message;
    }

    private async Task PrepareWarehouseOptionsAsync()
    {
        var warehouses = await warehouseAppService.GetWarehousesAsync(0, int.MaxValue);
        ViewBag.Warehouses = warehouses
            .Where(warehouse => warehouse.IsActive && warehouse.IsPhysical)
            .OrderBy(warehouse => warehouse.Name)
            .ToList();
    }

    private IActionResult RedirectBack(string? returnUrl, string fallbackAction)
        => !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl)
            ? Redirect(returnUrl)
            : RedirectToAction(fallbackAction);
}
