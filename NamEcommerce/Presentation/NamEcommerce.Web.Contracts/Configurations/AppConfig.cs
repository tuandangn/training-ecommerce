namespace NamEcommerce.Web.Contracts.Configurations;

[Serializable]
public sealed class AppConfig
{
    public bool AllowRegisterUser { get; set; }

    public int DefaultPageSize { get; set; }
    public int[] PageSizeOptions { get; set; } = [];

    public int UploadFileMaxSizeInBytes { get; set; }

    public string BreadcrumbSeparator { get; set; } = ">";

    public string N8nEndpoint { get; set; } = "";

    /// <summary>Danh sách % thuế VAT khả dụng khi nhận hàng (vd: [5, 8, 10, 15]).</summary>
    public decimal[] TaxRates { get; set; } = [5, 8, 10, 15];

    /// <summary>Tỉ lệ thuế mặc định (%).</summary>
    public decimal DefaultTaxRate { get; set; } = 10;
}
