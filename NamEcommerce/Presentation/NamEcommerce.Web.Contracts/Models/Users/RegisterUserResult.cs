namespace NamEcommerce.Web.Contracts.Models.Users;

[Serializable]
public sealed record RegisterUserResult
{
    public bool Success { get; init; }
    public Guid? CreatedId { get; set; }
    public string? ErrorMessage { get; set; }
}
