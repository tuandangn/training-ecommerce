using NamEcommerce.Web.Contracts.Models.DeliveryNotes;

namespace NamEcommerce.Web.Services.DeliveryNotes;

public interface IDeliveryRunModelFactory
{
    Task<DeliveryRunListModel> PrepareDeliveryRunListModelAsync(DeliveryRunSearchModel searchModel);
    Task<CreateDeliveryRunModel> PrepareCreateDeliveryRunModelAsync(CreateDeliveryRunModel? oldModel = null);
    Task<DeliveryRunDetailsModel> PrepareDeliveryRunDetailsModelAsync(Guid id);
    Task<DeliveryMobileIndexModel> PrepareDeliveryMobileIndexModelAsync(Guid currentUserId, string currentUserFullName);
    Task<DeliveryMobileRunModel> PrepareDeliveryMobileRunModelAsync(Guid id, Guid currentUserId, string currentUserFullName);
}
