using NamEcommerce.Web.Contracts.Models.Common;

namespace NamEcommerce.Web.Contracts.Commands.Models.Returns;

[Serializable]
public sealed class MoveVendorReturnToInspectingCommand : ICommand<CommonActionResultModel>
{
    public required Guid Id { get; init; }
}

[Serializable]
public sealed class ConfirmVendorReturnCommand : ICommand<CommonActionResultModel>
{
    public required Guid Id { get; init; }
    public Guid? WarehouseId { get; init; }
}

[Serializable]
public sealed class CancelVendorReturnCommand : ICommand<CommonActionResultModel>
{
    public required Guid Id { get; init; }
}

[Serializable]
public sealed class ReverseVendorReturnCommand : ICommand<CommonActionResultModel>
{
    public required Guid Id { get; init; }
    public required string Reason { get; init; }
}
