namespace NamEcommerce.Domain.Shared.Dtos.Inventory;

[Serializable]
public sealed record ProductReservationDto(Guid Id)
{
    public required Guid ProductId { get; init; }
    public required decimal TotalReservedByOrder { get; init; }
    public required DateTime UpdatedOnUtc { get; init; }
}
