namespace NamEcommerce.Domain.Shared.Dtos.Users;

[Serializable]
public sealed record RoleDto(Guid Id)
{
    public required string Name { get; init; }
    public required string NormalizedName { get; init; }
}
