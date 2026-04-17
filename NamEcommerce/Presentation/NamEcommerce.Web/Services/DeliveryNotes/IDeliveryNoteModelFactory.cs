using NamEcommerce.Web.Contracts.Models.DeliveryNotes;
using NamEcommerce.Web.Models.DeliveryNotes;

namespace NamEcommerce.Web.Services.DeliveryNotes;

public interface IDeliveryNoteModelFactory
{
    Task<DeliveryNoteListModel> PrepareDeliveryNoteListModelAsync(DeliveryNoteSearchModel searchModel);
    
    Task<CreateDeliveryNoteModel> PrepareCreateDeliveryNoteModelAsync(Guid orderId, CreateDeliveryNoteModel? model = null);
    
    Task<DeliveryNoteDetailsModel> PrepareDeliveryNoteDetailsModelAsync(Guid id);
}
