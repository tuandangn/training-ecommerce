using NamEcommerce.Web.Contracts.Models.DeliveryNotes;

namespace NamEcommerce.Web.Contracts.Services;

public interface IDeliveryNoteModelFactory
{
    Task<DeliveryNoteListModel> PrepareDeliveryNoteListModelAsync(DeliveryNoteSearchModel searchModel);
    
    Task<CreateDeliveryNoteModel> PrepareCreateDeliveryNoteModelAsync(Guid orderId);
    
    Task<DeliveryNoteDetailsModel> PrepareDeliveryNoteDetailsModelAsync(Guid id);
}
