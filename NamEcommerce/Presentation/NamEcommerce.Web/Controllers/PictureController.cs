using MediatR;
using Microsoft.AspNetCore.Mvc;
using NamEcommerce.Web.Contracts.Commands.Models.Media;
using NamEcommerce.Web.Contracts.Configurations;
using NamEcommerce.Web.Contracts.Queries.Models.Media;

namespace NamEcommerce.Web.Controllers;

public sealed class PictureController : BaseAuthorizedController
{
    private readonly IMediator _mediator;
    private readonly AppConfig _appConfig;

    public PictureController(IMediator mediator, AppConfig appConfig)
    {
        _mediator = mediator;
        _appConfig = appConfig;
    }

    /// <summary>
    /// Phục vụ ảnh dưới dạng file binary. Dùng cho src="/Picture/{id}".
    /// </summary>
    [HttpGet("/Picture/{id:guid}")]
    [ResponseCache(Duration = 3600, Location = ResponseCacheLocation.Client)]
    public async Task<IActionResult> Get(Guid id)
    {
        var file = await _mediator.Send(new GetPictureQuery { Id = id }).ConfigureAwait(false);
        if (file is null)
            return NotFound();

        return File(file.Data, file.MimeType);
    }

    /// <summary>
    /// Upload ảnh chứng từ qua AJAX. Trả về { success, id, dataUrl }.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Upload(IFormFile file)
    {
        if (file is null || file.Length == 0)
            return Json(new { success = false, message = "Vui lòng chọn file." });

        if (file.Length > _appConfig.UploadFileMaxSizeInBytes)
        {
            var maxMb = (int)Math.Floor(_appConfig.UploadFileMaxSizeInBytes / 1024m / 1024);
            return Json(new { success = false, message = $"Kích thước file tối đa là {maxMb} MB." });
        }

        var allowedMimes = new[] { "image/jpeg", "image/jpg", "image/png", "image/webp", "image/gif" };
        if (!allowedMimes.Contains(file.ContentType.ToLowerInvariant()))
            return Json(new { success = false, message = "Chỉ chấp nhận file ảnh (jpg, png, webp, gif)." });

        using var ms = new MemoryStream();
        await file.CopyToAsync(ms).ConfigureAwait(false);
        var data = ms.ToArray();

        var ext = Path.GetExtension(file.FileName).TrimStart('.');
        var result = await _mediator.Send(new UploadPictureCommand
        {
            Data = data,
            MimeType = file.ContentType,
            FileName = file.FileName,
            Extension = ext
        }).ConfigureAwait(false);

        if (!result.Success)
            return Json(new { success = false, message = result.ErrorMessage });

        return Json(new { success = true, id = result.PictureId, dataUrl = result.DataUrl });
    }
}
