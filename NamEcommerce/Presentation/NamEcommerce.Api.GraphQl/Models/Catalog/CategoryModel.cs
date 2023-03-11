namespace NamEcommerce.Api.GraphQl.Models.Catalog;

[Serializable]
public sealed record CategoryModel
{
    public CategoryModel(Guid id, string name)
        => (Id, Name) = (id, name);

    public Guid Id { get; }
    public string Name { get; }
}
