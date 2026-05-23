namespace NamEcommerce.Domain.Shared.Dtos.Common;

[Serializable]
public record struct IdPair(Guid FirstId, Guid SecondId)
{
    public bool IsValid() => FirstId != Guid.Empty && SecondId != Guid.Empty;

    public static implicit operator IdPair((Guid first, Guid second) pair) 
        => new(pair.first, pair.second);
}
