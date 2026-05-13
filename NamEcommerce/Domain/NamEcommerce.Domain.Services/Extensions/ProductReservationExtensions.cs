using NamEcommerce.Domain.Entities.Inventory;
using NamEcommerce.Domain.Shared.Dtos.Inventory;

namespace NamEcommerce.Domain.Services.Extensions;

internal static class ProductReservationExtensions
{
    public static ProductReservationDto ToDto(this ProductReservation entity) => new(entity.Id)
    {
        ProductId = entity.ProductId,
        TotalReservedByOrder = entity.TotalReservedByOrder,
        UpdatedOnUtc = entity.UpdatedOnUtc
    };
}
