using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NamEcommerce.Web.Contracts.Commands.Models.DeliveryNotes;
using NamEcommerce.Web.Contracts.Commands.Models.Media;
using NamEcommerce.Web.Contracts.Configurations;
using NamEcommerce.Web.Contracts.Security;
using NamEcommerce.Web.Contracts.Models.DeliveryNotes;
using NamEcommerce.Web.Services.DeliveryNotes;
using NamEcommerce.Domain.Shared.Services.Users;
using System.Text.Json;

namespace NamEcommerce.Web.Controllers;

[Authorize(Policy = SystemPermissions.DeliveryRuns.MobileAccess)]
public sealed class DeliveryMobileController(
    IDeliveryRunModelFactory deliveryRunModelFactory, ICurrentUserAccessor currentUserAccessor,
    AppConfig appConfig, IMediator mediator) : BaseAuthorizedController
{
    public async Task<IActionResult> Index(bool showCompleted = false)
    {
        var currentUser = await currentUserAccessor.GetCurrentUserAsync();
        if (currentUser is null)
            return RedirectToAction("Login", "User");

        var model = await deliveryRunModelFactory.PrepareDeliveryMobileIndexModelAsync(
            currentUser.Id,
            currentUser.FullName,
            showCompleted);
        return View(model);
    }

    public async Task<IActionResult> Run(Guid id)
    {
        var currentUser = await currentUserAccessor.GetCurrentUserAsync();
        if (currentUser is null)
            return RedirectToAction("Login", "User");

        try
        {
            var model = await deliveryRunModelFactory.PrepareDeliveryMobileRunModelAsync(id, currentUser.Id, currentUser.FullName);
            return View(model);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch
        {
            NotifyError("Error.DeliveryRunNotFound");
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpPost]
    public async Task<IActionResult> Cache(Guid id, string? deviceId)
    {
        var currentUser = await currentUserAccessor.GetCurrentUserAsync();
        if (currentUser is null)
            return Json(new { success = false, message = "Error.UserNotFound" });

        try
        {
            await deliveryRunModelFactory.PrepareDeliveryMobileRunModelAsync(id, currentUser.Id, currentUser.FullName);
        }
        catch (UnauthorizedAccessException)
        {
            return Json(new { success = false, message = "Error.Forbidden" });
        }
        catch
        {
            return Json(new { success = false, message = "Error.DeliveryRunNotFound" });
        }

        var result = await mediator.Send(new AcknowledgeDriverCacheDeliveryRunCommand(id, deviceId));
        return Json(new { success = result.Success, message = result.ErrorMessage });
    }

    [HttpPost]
    public async Task<IActionResult> CompleteDeliveryNote(
        Guid deliveryRunId,
        Guid deliveryNoteId,
        string? receiverName,
        string? note,
        double? latitude,
        double? longitude,
        string? locationAddress,
        string? idempotencyKey,
        decimal? cashCollectedAmount,
        string? acceptanceItemsJson,
        IFormFile? proofFile)
    {
        var currentUser = await currentUserAccessor.GetCurrentUserAsync();
        if (currentUser is null)
            return Json(new { success = false, message = "Error.UserNotFound" });

        DeliveryMobileRunModel run;
        try
        {
            run = await deliveryRunModelFactory.PrepareDeliveryMobileRunModelAsync(deliveryRunId, currentUser.Id, currentUser.FullName);
        }
        catch (UnauthorizedAccessException)
        {
            return Json(new { success = false, message = "Error.Forbidden" });
        }
        catch
        {
            return Json(new { success = false, message = "Error.DeliveryRunNotFound" });
        }
        if (!run.Run.Items.Any(item => item.DeliveryNoteId == deliveryNoteId))
            return Json(new { success = false, message = "Error.DeliveryNoteNotFound" });

        if (proofFile is null || proofFile.Length == 0)
            return Json(new { success = false, message = "Error.DeliveryProofRequired" });
        if (proofFile.Length > appConfig.UploadFileMaxSizeInBytes)
            return Json(new { success = false, message = "Error.UploadFileTooLarge" });
        if (!IsAllowedImage(proofFile.ContentType))
            return Json(new { success = false, message = "Error.InvalidImageType" });

        await using var stream = proofFile.OpenReadStream();
        using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream);

        var upload = await mediator.Send(new UploadPictureCommand
        {
            Data = memoryStream.ToArray(),
            MimeType = proofFile.ContentType,
            FileName = proofFile.FileName,
            Extension = Path.GetExtension(proofFile.FileName)
        });
        if (!upload.Success || !upload.PictureId.HasValue)
            return Json(new { success = false, message = upload.ErrorMessage ?? "Error.UploadPictureFailed" });

        var result = await mediator.Send(new CompleteMobileDeliveryNoteCommand
        {
            DeliveryRunId = deliveryRunId,
            DeliveryNoteId = deliveryNoteId,
            PictureId = upload.PictureId.Value,
            ReceiverName = receiverName,
            Latitude = latitude,
            Longitude = longitude,
            LocationAddress = locationAddress,
            Note = note,
            IdempotencyKey = idempotencyKey,
            CashCollectedAmount = cashCollectedAmount,
            Items = ParseAcceptanceItems(acceptanceItemsJson)
        });

        return Json(new { success = result.Success, message = result.ErrorMessage });
    }

    private static bool IsAllowedImage(string contentType)
        => contentType is "image/jpeg" or "image/jpg" or "image/png" or "image/webp" or "image/gif";

    private static IList<CompleteMobileDeliveryNoteItemCommand> ParseAcceptanceItems(string? acceptanceItemsJson)
    {
        if (string.IsNullOrWhiteSpace(acceptanceItemsJson))
            return [];

        try
        {
            return JsonSerializer.Deserialize<List<CompleteMobileDeliveryNoteItemCommand>>(
                acceptanceItemsJson,
                new JsonSerializerOptions(JsonSerializerDefaults.Web)) ?? [];
        }
        catch
        {
            return [];
        }
    }
}
