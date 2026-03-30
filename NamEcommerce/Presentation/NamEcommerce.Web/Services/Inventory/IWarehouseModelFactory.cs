using NamEcommerce.Web.Contracts.Models.Inventory;
using NamEcommerce.Web.Models.Inventory;

namespace NamEcommerce.Web.Services.Inventory;

public interface IWarehouseModelFactory
{
    Task<WarehouseListModel> PrepareWarehouseListModel(WarehouseListSearchModel searchModel);
    Task<CreateWarehouseModel> PrepareCreateWarehouseModel(CreateWarehouseModel? model = null);
    Task<EditWarehouseModel?> PrepareEditWarehouseModel(Guid id, EditWarehouseModel? model = null);
}
