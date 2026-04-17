namespace NamEcommerce.Web.Contracts.Models.DeliveryNotes;

[Serializable]
public sealed record DeliveryNoteLinkModel(Guid Id, string Code, int Status, DateTime CreatedOnUtc);
