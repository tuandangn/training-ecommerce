using NamEcommerce.Domain.Shared.Exceptions.GoodsReceipts;

namespace NamEcommerce.Domain.Shared.Dtos.GoodsReceipts;

[Serializable]
public abstract record BaseGoodsReceiptDto
{
    public required DateTime ReceivedOnUtc { get; init; }

    public string? TruckDriverName { get; set; }
    public string? TruckNumberSerial { get; set; }

    public required IEnumerable<Guid> PictureIds { get; init; }

    public string? Note { get; set; }

    /// <summary>Nhà cung cấp — nullable.</summary>
    public Guid? VendorId { get; set; }

    public virtual void Verify()
    {
        if (!PictureIds.Any())
            throw new GoodsReceiptProofPictureRequired();

        if (ReceivedOnUtc > DateTime.UtcNow)
            throw new GoodsReceiptItemDataIsInvalidException("Error.GoodsReceipt.ReceivedDateGreaterThanNow");
    }
}

[Serializable]
public sealed record GoodsReceiptDto(Guid Id) : BaseGoodsReceiptDto
{
    public required IEnumerable<GoodsReceiptItemDto> Items { get; init; }
    public bool IsPendingCosting { get; init; }

    // Vendor snapshot — chỉ có trong DTO đọc, không phải DTO ghi
    public string? VendorName { get; init; }
    public string? VendorPhone { get; init; }
    public string? VendorAddress { get; init; }

    // PurchaseOrder linkage
    public Guid? PurchaseOrderId { get; init; }
    public string? PurchaseOrderCode { get; init; }
}

[Serializable]
public sealed record CreateGoodsReceiptDto : BaseGoodsReceiptDto
{
    public required IList<AddGoodsReceiptItemDto> Items { get; init; }

    public override void Verify()
    {
        foreach (var item in Items)
            item.Verify();

        base.Verify();
    }
}

[Serializable]
public sealed record CreateGoodsReceiptResultDto
{
    public required Guid CreatedId { get; init; }
}

[Serializable]
public sealed record UpdateGoodsReceiptDto(Guid Id) : BaseGoodsReceiptDto;

[Serializable]
public sealed record UpdateGoodsReceiptResultDto
{
    public required Guid UpdatedId { get; init; }
}

[Serializable]
public sealed record DeleteGoodsReceiptDto(Guid GoodsReceiptId) : BaseGoodsReceiptDto;

[Serializable]
public sealed record SetGoodsReceiptVendorDto(Guid GoodsReceiptId)
{
    /// <summary>null = xoá vendor khỏi phiếu.</summary>
    public Guid? VendorId { get; init; }

    public void Verify()
    {
        if (GoodsReceiptId == Guid.Empty)
            throw new GoodsReceiptIsNotFoundException(GoodsReceiptId);
    }
}

[Serializable]
public sealed record SetGoodsReceiptVendorResultDto
{
    public required Guid UpdatedId { get; init; }
}
