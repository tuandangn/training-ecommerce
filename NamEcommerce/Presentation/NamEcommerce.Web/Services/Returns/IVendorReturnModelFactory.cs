using NamEcommerce.Web.Contracts.Models.Returns;
using NamEcommerce.Web.Models.Returns;

namespace NamEcommerce.Web.Services.Returns;

public interface IVendorReturnModelFactory
{
    Task<VendorReturnListModel> PrepareVendorReturnListModel(VendorReturnListSearchModel searchModel);
    Task<CreateVendorReturnModel> PrepareCreateVendorReturnModel(CreateVendorReturnModel? model = null);
    Task<VendorReturnDetailsModel?> PrepareVendorReturnDetailsModel(Guid id);
}
