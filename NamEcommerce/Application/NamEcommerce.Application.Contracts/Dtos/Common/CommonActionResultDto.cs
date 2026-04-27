namespace NamEcommerce.Application.Contracts.Dtos.Common;

[Serializable]
public record CommonActionResultDto
{
    public required bool Success { get; set; }
    public string? ErrorMessage { get; set; }

    public void Deconstruct(out bool success, out string? errorMessage)
        => (success, errorMessage) = (Success, ErrorMessage);

    private static readonly Lazy<CommonActionResultDto> _successInstance = new(() => new CommonActionResultDto { Success = true });
    public static CommonActionResultDto CreateSuccess() => _successInstance.Value;
    public static CommonActionResultDto CreateError(string? errorMessage) => new CommonActionResultDto { Success = false, ErrorMessage = errorMessage };
}
