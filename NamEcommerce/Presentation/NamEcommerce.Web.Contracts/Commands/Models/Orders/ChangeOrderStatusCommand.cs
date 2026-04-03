using MediatR;

namespace NamEcommerce.Web.Contracts.Commands.Models.Orders;

[Serializable]
public sealed class ChangeOrderStatusCommand : IRequest<ChangeOrderStatusResultModel>
{
    public required Guid OrderId { get; init; }
    public required int Status { get; init; }
}

[Serializable]
public sealed record ChangeOrderStatusResultModel
{
    public required bool Success { get; init; }
    public string? ErrorMessage { get; init; }
}
