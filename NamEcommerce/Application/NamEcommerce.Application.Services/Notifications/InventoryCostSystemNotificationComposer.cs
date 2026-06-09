using NamEcommerce.Application.Contracts.Dtos.Notifications;
using NamEcommerce.Domain.Shared.Enums.Notifications;

namespace NamEcommerce.Application.Services.Notifications;

public static class InventoryCostSystemNotificationComposer
{
    private const string ViewCostDetailsPermission = "Inventory.ViewCostDetails";

    public static CreateSystemNotificationAppDto ReturnReversalLost(Guid productId, int affectedNoteCount)
        => new()
        {
            Type = (int)SystemNotificationType.InventoryCostReturnReversalLost,
            Severity = (int)SystemNotificationSeverity.Warning,
            Title = "Giá vốn hàng trả cần kiểm tra lại",
            Message = $"Có {affectedNoteCount} phiếu xuất bị ảnh hưởng sau khi tái định giá — dữ liệu bù trừ giá vốn hàng trả bị mất. Vui lòng chạy lại tái định giá hoặc kiểm tra thủ công.",
            RequiredPermission = ViewCostDetailsPermission,
            RelatedEntityId = productId,
            RelatedEntityType = "Product",
            ActionUrl = $"/Inventory/CostHistory?productId={productId}"
        };
}
