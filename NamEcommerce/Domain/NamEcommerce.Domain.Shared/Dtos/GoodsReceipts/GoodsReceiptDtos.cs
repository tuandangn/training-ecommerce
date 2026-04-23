using NamEcommerce.Domain.Shared.Exceptions.GoodsReceipts;

namespace NamEcommerce.Domain.Shared.Dtos.GoodsReceipts;

[Serializable]
public abstract record BaseGoodsReceiptDto
{
    public required DateTime CreatedOnUtc { get; init; }

    public string? TruckDriverName { get; set; }
    public string? TruckNumberSerial { get; set; }

    public required IEnumerable<Guid> PictureIds { get; init; }

    public string? Note { get; set; }

    public virtual void Verify()
    {
        if (!PictureIds.Any())
            throw new GoodsReceiptProofPictureRequired();
    }
}

[Serializable]
public sealed record GoodsReceiptDto(Guid Id) : BaseGoodsReceiptDto
{
    public required IEnumerable<GoodsReceiptItemDto> Items { get; init; }
    public bool IsPendingCosting { get; init; }
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
