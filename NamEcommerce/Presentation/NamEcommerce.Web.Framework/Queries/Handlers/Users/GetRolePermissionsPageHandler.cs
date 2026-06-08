using MediatR;
using NamEcommerce.Application.Contracts.Security;
using NamEcommerce.Application.Contracts.Users;
using NamEcommerce.Web.Contracts.Models.Users;
using NamEcommerce.Web.Contracts.Queries.Models.Users;

namespace NamEcommerce.Web.Framework.Queries.Handlers.Users;

public sealed class GetRolePermissionsPageHandler(
    IPermissionAppService permissionAppService,
    IUserAppService userAppService)
    : IRequestHandler<GetRolePermissionsPageQuery, RolePermissionsPageModel?>
{
    private static readonly Dictionary<string, string> GroupLabels = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Catalog"] = "Hàng hóa & Danh mục",
        ["Orders"] = "Bán hàng",
        ["DeliveryNotes"] = "Xuất kho",
        ["DeliveryRuns"] = "Chuyến giao",
        ["DirectShip"] = "Giao thẳng",
        ["PurchaseOrders"] = "Đơn nhập",
        ["GoodsReceipts"] = "Nhập kho",
        ["VendorReturns"] = "Trả hàng NCC",
        ["Inventory"] = "Tồn kho",
        ["Warehouses"] = "Kho hàng",
        ["Customers"] = "Khách hàng",
        ["CustomerReturns"] = "Khách trả hàng",
        ["Debts"] = "Công nợ",
        ["Finance"] = "Tài chính",
        ["Users"] = "Hệ thống",
    };

    private static readonly Dictionary<string, string> PermissionLabels = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Catalog.Categories.View"] = "Xem danh mục",
        ["Catalog.Categories.Manage"] = "Quản lý danh mục",
        ["Catalog.Products.View"] = "Xem hàng hóa",
        ["Catalog.Products.Manage"] = "Quản lý hàng hóa",
        ["Catalog.Vendors.View"] = "Xem nhà cung cấp",
        ["Catalog.Vendors.Manage"] = "Quản lý nhà cung cấp",
        ["Catalog.UnitMeasurements.Manage"] = "Quản lý đơn vị tính",
        ["Orders.View"] = "Xem đơn hàng",
        ["Orders.Create"] = "Tạo đơn hàng",
        ["Orders.Edit"] = "Sửa đơn hàng",
        ["Orders.Cancel"] = "Hủy đơn hàng",
        ["Orders.FastSale"] = "Bán nhanh",
        ["DeliveryNotes.View"] = "Xem phiếu xuất kho",
        ["DeliveryNotes.Manage"] = "Quản lý phiếu xuất kho",
        ["DeliveryRuns.View"] = "Xem chuyến giao",
        ["DeliveryRuns.Manage"] = "Quản lý chuyến giao",
        ["DeliveryRuns.ConfirmCashHandover"] = "Xác nhận bàn giao tiền",
        ["DeliveryRuns.MobileAccess"] = "Dùng app giao hàng",
        ["DirectShip.View"] = "Xem hàng giao thẳng",
        ["DirectShip.Manage"] = "Quản lý giao thẳng",
        ["PurchaseOrders.View"] = "Xem đơn nhập",
        ["PurchaseOrders.Create"] = "Tạo/sửa đơn nhập",
        ["PurchaseOrders.Cancel"] = "Hủy đơn nhập",
        ["GoodsReceipts.Manage"] = "Quản lý nhập kho",
        ["VendorReturns.Manage"] = "Trả hàng nhà cung cấp",
        ["Inventory.View"] = "Xem tồn kho",
        ["Inventory.Adjust"] = "Điều chỉnh tồn kho",
        ["Inventory.Transfer"] = "Chuyển kho",
        ["Inventory.ViewCostDetails"] = "Xem giá vốn",
        ["Inventory.Preparation"] = "Chuẩn bị hàng",
        ["Warehouses.Manage"] = "Quản lý kho hàng",
        ["Customers.View"] = "Xem khách hàng",
        ["Customers.Manage"] = "Quản lý khách hàng",
        ["CustomerReturns.Manage"] = "Xử lý khách trả hàng",
        ["Debts.CustomerDebts.View"] = "Xem công nợ khách hàng",
        ["Debts.CustomerDebts.RecordPayment"] = "Thu tiền khách hàng",
        ["Debts.CustomerRefunds.Manage"] = "Hoàn tiền khách hàng",
        ["Debts.VendorDebts.View"] = "Xem công nợ NCC",
        ["Debts.VendorDebts.RecordPayment"] = "Trả tiền NCC",
        ["Finance.Expenses.View"] = "Xem chi phí",
        ["Finance.Expenses.Manage"] = "Quản lý chi phí",
        ["Finance.Accounting"] = "Kế toán",
        ["Finance.Reports.Financial"] = "Báo cáo tài chính",
        ["Finance.Reports.DirectShip"] = "Báo cáo direct-ship",
        ["Users.ManageRoles"] = "Quản lý phân quyền",
    };

    public async Task<RolePermissionsPageModel?> Handle(GetRolePermissionsPageQuery request, CancellationToken cancellationToken)
    {
        var roles = (await userAppService.GetRolesAsync().ConfigureAwait(false))
            .Where(r => !string.Equals(r.Name, "Admin", StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (roles.Count == 0)
            return null;

        var selectedRoleId = request.RoleId != Guid.Empty && roles.Any(r => r.Id == request.RoleId)
            ? request.RoleId
            : roles[0].Id;

        var rolePermissions = await permissionAppService.GetRolePermissionsAsync(selectedRoleId, cancellationToken).ConfigureAwait(false);
        if (rolePermissions is null)
            return null;

        var groups = rolePermissions.AllPermissions
            .GroupBy(p => p.Name.Split('.')[0])
            .OrderBy(g => g.Key)
            .Select(g => new PermissionGroupModel
            {
                GroupName = GroupLabels.TryGetValue(g.Key, out var label) ? label : g.Key,
                Items = g.Select(p => new PermissionItemModel
                {
                    Id = p.Id,
                    Name = p.Name,
                    Label = PermissionLabels.TryGetValue(p.Name, out var pLabel) ? pLabel : p.Name,
                    IsGranted = rolePermissions.GrantedPermissionIds.Contains(p.Id)
                }).ToList()
            })
            .ToList();

        return new RolePermissionsPageModel
        {
            SelectedRoleId = selectedRoleId,
            AvailableRoles = roles.Select(r => new RoleSelectionModel
            {
                Id = r.Id,
                Name = r.Name,
                Selected = r.Id == selectedRoleId
            }).ToList(),
            CurrentRole = new RolePermissionsModel
            {
                RoleId = rolePermissions.RoleId,
                RoleName = rolePermissions.RoleName,
                Groups = groups
            }
        };
    }
}
