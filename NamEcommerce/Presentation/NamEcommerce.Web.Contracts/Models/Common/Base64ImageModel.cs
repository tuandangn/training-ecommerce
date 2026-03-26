namespace NamEcommerce.Web.Contracts.Models.Common;

[Serializable]
public sealed record Base64ImageModel
{
    public string? Base64Data { get; set; }
    public string? FileName { get; set; }
    public string? Extension { get; set; }

    public byte[]? GetData()
    {
        if (string.IsNullOrEmpty(Base64Data))
            return null;

        var parts = Base64Data.Split(',');
        if (parts.Length != 2)
            return null;

        var dataPart = parts[1];
        return Convert.FromBase64String(dataPart);
    }

    public string? GetMimeType()
    {
        if (string.IsNullOrEmpty(Base64Data))
            return null;

        var parts = Base64Data.Split(',');
        if (parts.Length != 2)
            return null;

        var startIndex = Base64Data.IndexOf(':');
        var endIndex = Base64Data.IndexOf(';');
        if (startIndex == -1 || endIndex == -1)
            return null;

        return Base64Data.Substring(startIndex + 1, endIndex - startIndex - 1);
    }
}
