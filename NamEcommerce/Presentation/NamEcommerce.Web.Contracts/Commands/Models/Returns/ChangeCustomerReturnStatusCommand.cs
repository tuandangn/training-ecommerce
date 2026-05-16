using MediatR;
using NamEcommerce.Web.Contracts.Models.Common;

namespace NamEcommerce.Web.Contracts.Commands.Models.Returns;

[Serializable]
public sealed class MoveCustomerReturnToInspectingCommand : IRequest<CommonActionResultModel>
{
    public required Guid Id { get; init; }
}

[Serializable]
public sealed class ConfirmCustomerReturnCommand : IRequest<CommonActionResultModel>
{
    public required Guid Id { get; init; }
}

[Serializable]
public sealed class CancelCustomerReturnCommand : IRequest<CommonActionResultModel>
{
    public required Guid Id { get; init; }
}
