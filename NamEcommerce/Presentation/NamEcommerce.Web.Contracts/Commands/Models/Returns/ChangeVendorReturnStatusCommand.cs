using MediatR;
using NamEcommerce.Web.Contracts.Models.Common;

namespace NamEcommerce.Web.Contracts.Commands.Models.Returns;

[Serializable]
public sealed class MoveVendorReturnToInspectingCommand : IRequest<CommonActionResultModel>
{
    public required Guid Id { get; init; }
}

[Serializable]
public sealed class ConfirmVendorReturnCommand : IRequest<CommonActionResultModel>
{
    public required Guid Id { get; init; }
    public Guid? WarehouseId { get; init; }
}

[Serializable]
public sealed class CancelVendorReturnCommand : IRequest<CommonActionResultModel>
{
    public required Guid Id { get; init; }
}

[Serializable]
public sealed class ReverseVendorReturnCommand : IRequest<CommonActionResultModel>
{
    public required Guid Id { get; init; }
    public required string Reason { get; init; }
}
