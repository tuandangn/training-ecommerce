using NamEcommerce.Web.Contracts.Models.Common;

namespace NamEcommerce.Web.Models.FastSales;

[Serializable]
public sealed class FastSaleModel
{
    public IEnumerable<EntityOptionListModel.EntityOptionModel> Customers { get; init; } = [];
    public IEnumerable<EntityOptionListModel.EntityOptionModel> Warehouses { get; init; } = [];
    public Guid? DefaultCustomerId { get; init; }
    public bool BankTransferEnabled { get; init; }
    public string BankAccountLabel { get; init; } = string.Empty;
    public bool ManualBankTransferConfirmEnabled { get; init; }
}
