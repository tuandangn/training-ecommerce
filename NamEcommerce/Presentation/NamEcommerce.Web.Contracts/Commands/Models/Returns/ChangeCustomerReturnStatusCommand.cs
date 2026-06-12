using NamEcommerce.Web.Contracts.Models.Common;

namespace NamEcommerce.Web.Contracts.Commands.Models.Returns;

[Serializable]
public sealed class MoveCustomerReturnToInspectingCommand : ICommand<CommonActionResultModel>
{
    public required Guid Id { get; init; }
}

[Serializable]
public sealed class ConfirmCustomerReturnCommand : ICommand<CommonActionResultModel>
{
    public required Guid Id { get; init; }
    public Guid? WarehouseId { get; init; }
}

[Serializable]
public sealed class CancelCustomerReturnCommand : ICommand<CommonActionResultModel>
{
    public required Guid Id { get; init; }
}
