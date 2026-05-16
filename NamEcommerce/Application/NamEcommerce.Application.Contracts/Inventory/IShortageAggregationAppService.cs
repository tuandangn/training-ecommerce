using NamEcommerce.Application.Contracts.Dtos.Inventory;

namespace NamEcommerce.Application.Contracts.Inventory;

public interface IShortageAggregationAppService
{
    Task<ShortageAggregationAppDto> GetAggregatedShortagesAsync(ShortageAggregationFilterAppDto filter);
    Task<IList<ExistingDraftPurchaseOrderAppDto>> CheckExistingDraftsAsync(IList<Guid> vendorIds);
    Task<IList<RelatedPurchaseOrderAppDto>> CheckRelatedPurchaseOrdersAsync(CheckRelatedPurchaseOrdersAppDto dto);
    Task<CreatePosFromShortageResultAppDto> CreatePurchaseOrdersFromShortageAsync(CreatePosFromShortageAppDto dto);
}
