namespace NamEcommerce.Web.Models.Orders;

[Serializable]
public sealed record OrderDetailsModel
{
    public required Guid Id { get; init; }
    public required string Code { get; init; }
    public required decimal OrderSubTotal { get; init; }
    public required decimal TotalAmount { get; init; }
    public required Guid CustomerId { get; init; }

    public string? CustomerName { get; set; }
    public string? CustomerAddress { get; set; }
    public string? CustomerPhoneNumber { get; set; }

    public decimal OrderDiscount { get; set; }
    public int Status { get; set; }
    public string? LockOrderReason { get; set; }

    public string? Note { get; set; }

    public DateTime? ExpectedShippingDate { get; set; }
    public string? ShippingAddress { get; set; }

    public bool CanUpdateInfo { get; init; }
    public bool CanLockOrder { get; init; }
    public bool CanUpdateOrderItems { get; init; }

    public IList<OrderItemModel> Items { get; init; } = [];
    public IList<DeliveryNoteBasicModel> DeliveryNotes { get; init; } = [];

    /// <summary>
    /// Checks if all order items have been fully covered by delivery notes
    /// </summary>
    public bool AreAllItemsFullyCovered
    {
        get
        {
            if (Items.Count == 0)
                return false;

            foreach (var item in Items)
            {
                var totalDeliveredQty = DeliveryNotes
                    .SelectMany(dn => dn.Items)
                    .Where(dni => dni.OrderItemId == item.Id)
                    .Sum(dni => dni.Quantity);

                if (totalDeliveredQty < item.Quantity)
                    return false;
            }

            return true;
        }
    }

    [Serializable]
    public sealed record OrderItemModel(Guid Id)
    {
        public required Guid ProductId { get; init; }
        public required decimal Quantity { get; init; }
        public required decimal UnitPrice { get; init; }
        public string? ProductName { get; set; }
        public string? ProductPicture { get; set; }
        public decimal? ProductAvailableQty { get; set; }
        public decimal SubTotal => UnitPrice * Quantity;

        /// <summary>
        /// Gets the total quantity already delivered for this item across all delivery notes
        /// </summary>
        public decimal GetDeliveredQuantity(IList<DeliveryNoteBasicModel> deliveryNotes)
        {
            return deliveryNotes
                .SelectMany(dn => dn.Items)
                .Where(dni => dni.OrderItemId == Id)
                .Sum(dni => dni.Quantity);
        }

        /// <summary>
        /// Gets the remaining quantity that can be delivered for this item
        /// </summary>
        public decimal GetRemainingQuantity(IList<DeliveryNoteBasicModel> deliveryNotes)
        {
            var delivered = GetDeliveredQuantity(deliveryNotes);
            return Quantity - delivered;
        }
    }

    [Serializable]
    public sealed record DeliveryNoteBasicModel
    {
        public required Guid Id { get; init; }
        public required string Code { get; init; }
        public IList<DeliveryNoteItemModel> Items { get; init; } = [];
    }

    [Serializable]
    public sealed record DeliveryNoteItemModel
    {
        public required Guid OrderItemId { get; init; }
        public required decimal Quantity { get; init; }
    }
}

