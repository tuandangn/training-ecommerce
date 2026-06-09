using NamEcommerce.Web.Contracts.Models.Debts;
using NamEcommerce.Web.Models.Debts;

namespace NamEcommerce.Web.Services.Debts;

public interface IVendorRefundModelFactory
{
    Task<VendorRefundListSearchModel> PrepareRefundListSearchModel(VendorRefundListSearchModel? model = null);
    Task<VendorRefundDetailsViewModel?> PrepareRefundDetailsModel(Guid id);
}
