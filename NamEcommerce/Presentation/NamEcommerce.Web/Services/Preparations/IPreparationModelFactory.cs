using NamEcommerce.Web.Models.Preparations;

namespace NamEcommerce.Web.Services.Preparations;

public interface IPreparationModelFactory
{
    Task<ProductPreparationListModel> PrepareProductPreparationListModelAsync(PreparationListSearchModel searchModel);
    Task<CustomerPreparationListModel> PrepareCustomerPreparationListModelAsync(PreparationListSearchModel searchModel);
}
