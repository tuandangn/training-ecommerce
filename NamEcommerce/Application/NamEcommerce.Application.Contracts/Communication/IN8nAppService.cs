namespace NamEcommerce.Application.Contracts.Communication;

public interface IN8nAppService
{
    Task NotifyDeliveryNoteIsConfirmed(Guid id);
}
