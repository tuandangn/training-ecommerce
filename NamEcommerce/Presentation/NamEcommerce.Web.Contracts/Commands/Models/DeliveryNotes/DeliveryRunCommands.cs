using MediatR;
using NamEcommerce.Web.Contracts.Models.Common;

namespace NamEcommerce.Web.Contracts.Commands.Models.DeliveryNotes;

[Serializable]
public sealed class CreateDeliveryRunCommand : IRequest<CreateDeliveryRunResultModel>
{
    public Guid AssignedDeliveryUserId { get; set; }
    public IList<Guid> DeliveryNoteIds { get; set; } = [];
    public string? Note { get; set; }
}

[Serializable]
public sealed class CreateDeliveryRunResultModel
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public string? SuccessMessage { get; set; }
    public Guid? CreatedId { get; set; }
}

[Serializable]
public sealed record IssuePaperManifestDeliveryRunCommand(Guid Id) : IRequest<CommonActionResultModel>;

[Serializable]
public sealed record AcknowledgeDriverCacheDeliveryRunCommand(Guid Id, string? DeviceId) : IRequest<CommonActionResultModel>;

[Serializable]
public sealed record HandOverDeliveryRunCommand(Guid Id) : IRequest<CommonActionResultModel>;

[Serializable]
public sealed record CloseDeliveryRunCommand(Guid Id) : IRequest<CommonActionResultModel>;

[Serializable]
public sealed record ConfirmDeliveryRunCashHandoverCommand(Guid Id, decimal Amount, string? Note) : IRequest<CommonActionResultModel>;

[Serializable]
public sealed record CancelDeliveryRunCommand(Guid Id) : IRequest<CommonActionResultModel>;
