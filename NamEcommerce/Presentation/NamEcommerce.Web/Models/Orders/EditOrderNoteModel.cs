namespace NamEcommerce.Web.Models.Orders;

[Serializable]
public sealed class EditOrderNoteModel
{
    public Guid OrderId { get; set; }
    public string? Note { get; set; }
}
