using NamEcommerce.Web.Contracts.Models.Returns;
using NamEcommerce.Web.Models.Returns;

namespace NamEcommerce.Web.Services.Returns;

public interface ICustomerReturnModelFactory
{
    Task<CustomerReturnListModel> PrepareCustomerReturnListModel(CustomerReturnListSearchModel searchModel);
    Task<CreateCustomerReturnModel> PrepareCreateCustomerReturnModel(CreateCustomerReturnModel? model = null);

    /// <summary>
    /// Lấy model chi tiết phiếu trả hàng khách — trả về <c>null</c> nếu không tìm thấy.
    /// </summary>
    Task<CustomerReturnModel?> PrepareCustomerReturnDetailsModel(Guid id);
}
