using NamEcommerce.Web.Contracts.Models.Debts;
using NamEcommerce.Web.Models.Debts;

namespace NamEcommerce.Web.Services.Debts;

public interface ICustomerRefundModelFactory
{
    Task<CustomerRefundListSearchModel> PrepareRefundListSearchModel(CustomerRefundListSearchModel? model = null);
    Task<CustomerRefundDetailsViewModel?> PrepareRefundDetailsModel(Guid id);
}
