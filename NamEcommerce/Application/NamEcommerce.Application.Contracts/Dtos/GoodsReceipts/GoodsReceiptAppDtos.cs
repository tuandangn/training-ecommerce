namespace NamEcommerce.Application.Contracts.Dtos.GoodsReceipts;

[Serializable]
public abstract record BaseGoodsReceiptAppDto
{
    public required DateTime CreatedOnUtc { get; init; }

    public string? TruckDriverName { get; set; }
    public string? TruckNumberSerial { get; set; }

    public required IEnumerable<Guid> PictureIds { get; init; }

    public string? Note { get; set; }

    public virtual (bool valid, string? errorMessage) Validate()
    {
        if (!PictureIds.Any())
            return (false, "Error.GoodsReceipt.ProofPictureRequired");

        return (true, null);
    }
}

[Serializable]
public sealed record GoodsReceiptAppDto(Guid Id) : BaseGoodsReceiptAppDto
{
    public IList<GoodsReceiptItemAppDto> Items { get; } = [];
    public bool IsPendingCosting { get; init; }
}

[Serializable]
public sealed record CreateGoodsReceiptAppDto : BaseGoodsReceiptAppDto
{
    public required IList<CreateGoodsReceiptItemAppDto> Items { get; init; }

    public override (bool valid, string? errorMessage) Validate()
    {
        if (Items is null || Items.Count == 0)
            return (false, "Error.GoodsReceipt.ItemsRequired");

        foreach (var item in Items)
        {
            var (valid, errorMessage) = item.Validate();
            if (!valid)
                return (valid, errorMessage);
        }

        return base.Validate();
    }
}

[Serializable]
public sealed record CreateGoodsReceiptResultAppDto
{
    public required bool Success { get; init; }
    public Guid? CreatedId { get; set; }
    public string? ErrorMessage { get; set; }
}

[Serializable]
public sealed record UpdateGoodsReceiptAppDto(Guid Id) : BaseGoodsReceiptAppDto;

[Serializable]
public sealed record UpdateGoodsReceiptResultAppDto
{
    public required bool Success { get; init; }
    public Guid? UpdatedId { get; set; }
    public string? ErrorMessage { get; set; }
}
