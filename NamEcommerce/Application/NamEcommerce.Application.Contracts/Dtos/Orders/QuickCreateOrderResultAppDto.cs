using NamEcommerce.Application.Contracts.Dtos.Common;

namespace NamEcommerce.Application.Contracts.Dtos.Orders;

[Serializable]
public sealed record QuickCreateOrderResultAppDto : CommonActionResultDto
{
    public Guid? OrderId { get; init; }

    public static new QuickCreateOrderResultAppDto CreateError(string? errorMessage)
        => new() { Success = false, ErrorMessage = errorMessage };
}

