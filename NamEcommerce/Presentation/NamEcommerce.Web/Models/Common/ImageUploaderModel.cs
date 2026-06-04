namespace NamEcommerce.Web.Models.Common;

public sealed class ImageUploaderModel
{
    public string FieldName { get; set; } = "PictureIds";
    public string? Label { get; set; }
    public string? HintText { get; set; }
    public int MaxFiles { get; set; } = 1;
    public bool Required { get; set; } = false;
    public string RequiredMessage { get; set; } = "Vui lòng tải lên ít nhất 1 hình ảnh.";
    public IList<Guid> ExistingPictureIds { get; set; } = [];
}
