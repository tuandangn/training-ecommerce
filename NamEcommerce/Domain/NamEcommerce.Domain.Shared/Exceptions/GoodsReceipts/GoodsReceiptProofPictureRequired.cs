namespace NamEcommerce.Domain.Shared.Exceptions.GoodsReceipts;

[Serializable]
public sealed class GoodsReceiptProofPictureRequired() : NamEcommerceDomainException("Error.GoodsReceipt.Picture.Required");

