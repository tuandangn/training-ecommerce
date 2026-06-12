using NamEcommerce.Web.Contracts.Models.Common;

namespace NamEcommerce.Web.Contracts.Commands.Models.DeliveryNotes;

[Serializable]
public sealed class CreateDeliveryRunCommand : ICommand<CreateDeliveryRunResultModel>
{
    public Guid AssignedDeliveryUserId { get; set; }
    public IList<Guid> DeliveryNoteIds { get; set; } = [];
    public string? Note { get; set; }
}

[Serializable]
public sealed class CreateDeliveryRunResultModel : ICommandResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public string? SuccessMessage { get; set; }
    public Guid? CreatedId { get; set; }
}

[Serializable]
public sealed record IssuePaperManifestDeliveryRunCommand(Guid Id) : ICommand<CommonActionResultModel>;

[Serializable]
public sealed record AcknowledgeDriverCacheDeliveryRunCommand(Guid Id, string? DeviceId) : ICommand<CommonActionResultModel>;

[Serializable]
public sealed record HandOverDeliveryRunCommand(Guid Id) : ICommand<CommonActionResultModel>;

[Serializable]
public sealed record CloseDeliveryRunCommand(Guid Id) : ICommand<CommonActionResultModel>;

[Serializable]
public sealed record ConfirmDeliveryRunCashHandoverCommand(Guid Id, decimal Amount, string? Note) : ICommand<CommonActionResultModel>;

[Serializable]
public sealed record CancelDeliveryRunCommand(Guid Id) : ICommand<CommonActionResultModel>;
