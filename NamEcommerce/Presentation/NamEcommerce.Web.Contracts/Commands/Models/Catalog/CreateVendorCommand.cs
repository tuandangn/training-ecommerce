using NamEcommerce.Web.Contracts.Models.Catalog;

namespace NamEcommerce.Web.Contracts.Commands.Models.Catalog;

[Serializable]
public sealed class CreateVendorCommand : ICommand<CreateVendorResultModel>
{
    public required string Name { get; init; }
    public required string PhoneNumber { get; init; }
    public string? Address { get; set; }
    public int DisplayOrder { get; set; }
    /// <summary>Công nợ ban đầu — nếu > 0 sẽ tạo một phiếu VendorDebt số dư đầu kỳ.</summary>
    public decimal? InitialDebt { get; set; }
}
