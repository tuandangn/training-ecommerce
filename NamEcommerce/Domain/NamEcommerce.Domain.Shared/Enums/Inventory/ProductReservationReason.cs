namespace NamEcommerce.Domain.Shared.Enums.Inventory;

public enum ProductReservationReason
{
    OrderCreated = 1,
    OrderItemAdded = 2,
    OrderItemIncreased = 3,
    OrderItemDecreased = 4,
    OrderItemRemoved = 5,
    OrderCancelled = 6,
    OrderDeleted = 7,
    OrderLocked = 8,
    DeliveryNoteConfirmed = 9,
    DeliveryNoteCancelled = 10,
    MigrationBackfill = 99
}
