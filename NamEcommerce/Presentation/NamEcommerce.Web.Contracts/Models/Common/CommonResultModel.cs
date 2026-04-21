namespace NamEcommerce.Web.Contracts.Models.Common;

[Serializable]
public sealed record CommonResultModel
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public string? SuccessMessage { get; set; }

    public void Deconstruct(out bool success, out string? errorMessage)
        => (success, errorMessage) = (Success, ErrorMessage);
}
