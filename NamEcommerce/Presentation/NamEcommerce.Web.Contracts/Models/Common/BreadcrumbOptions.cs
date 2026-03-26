namespace NamEcommerce.Web.Contracts.Models.Common;

[Serializable]
public record struct BreadcrumbOptions
{
    public BreadcrumbOptions()
    {
        Separator = ">>>";
    }

    public bool Disabled { get; set; }
    public string Separator { get; set; }
    public bool ExcludeCurrent { get; set; }
}
