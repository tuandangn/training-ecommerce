using NamEcommerce.Web.Contracts.Models.Returns;
using NamEcommerce.Web.Models.Returns;

namespace NamEcommerce.Web.Services.Returns;

public interface IVendorReturnModelFactory
{
    Task<VendorReturnListModel> PrepareVendorReturnListModel(VendorReturnListSearchModel searchModel);
    Task<CreateVendorReturnModel> PrepareCreateVendorReturnModel(CreateVendorReturnModel? model = null);
    /// <summary>
    /// Lấy model chi tiết phiếu trả hàng NCC — trả về <c>null</c> nếu không tìm thấy.
    /// </summary>
    Task<VendorReturnModel?> PrepareVendorReturnDetailsModel(Guid id);
}
