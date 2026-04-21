namespace NamEcommerce.Application.Contracts.Dtos.Common;

[Serializable]
public record CommonActionResultDto
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }

    public void Deconstruct(out bool success, out string? errorMessage)
        => (success, errorMessage) = (Success, ErrorMessage);
}
