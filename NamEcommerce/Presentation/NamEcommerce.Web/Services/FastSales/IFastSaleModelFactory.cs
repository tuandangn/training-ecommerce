using NamEcommerce.Web.Models.FastSales;

namespace NamEcommerce.Web.Services.FastSales;

public interface IFastSaleModelFactory
{
    Task<OrderQuickCreateModel> PrepareFastSaleModelAsync();
}
