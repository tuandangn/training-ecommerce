using NamEcommerce.Web.Contracts.Models.Returns;
using NamEcommerce.Web.Models.Returns;

namespace NamEcommerce.Web.Services.Returns;

public interface ICustomerReturnModelFactory
{
    Task<CustomerReturnListModel> PrepareCustomerReturnListModel(CustomerReturnListSearchModel searchModel);
    Task<CreateCustomerReturnModel> PrepareCreateCustomerReturnModel(CreateCustomerReturnModel? model = null);
    Task<CustomerReturnDetailsModel?> PrepareCustomerReturnDetailsModel(Guid id);
}
