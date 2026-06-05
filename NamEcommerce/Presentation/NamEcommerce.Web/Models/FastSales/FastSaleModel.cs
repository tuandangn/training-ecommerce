using NamEcommerce.Web.Contracts.Models.Common;

namespace NamEcommerce.Web.Models.FastSales;

[Serializable]
public sealed class FastSaleModel
{
    public IEnumerable<EntityOptionListModel.EntityOptionModel> Warehouses { get; init; } = [];
    public Guid? DefaultCustomerId { get; init; }
    public string DefaultCustomerName { get; init; } = string.Empty;
    public string DefaultCustomerPhone { get; init; } = string.Empty;
    public string DefaultCustomerAddress { get; init; } = string.Empty;
    public bool BankTransferEnabled { get; init; }
    public string BankAccountLabel { get; init; } = string.Empty;
    public bool ManualBankTransferConfirmEnabled { get; init; }
}
