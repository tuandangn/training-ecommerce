using NamEcommerce.Domain.Entities.Security;
using NamEcommerce.Domain.Entities.Users;
using NamEcommerce.Data.Contracts;
using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Web.Contracts.Security;

namespace NamEcommerce.Web.Services.Seeding;

public sealed class SystemPermissionsSeeder(
    IEntityDataReader<Permission> permissionReader,
    IEntityDataReader<Role> roleReader,
    IEntityDataReader<RolePermission> rolePermissionReader,
    IRepository<Permission> permissionRepository,
    IRepository<RolePermission> rolePermissionRepository) : IDataSeeder
{
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await SeedPermissionsAsync(cancellationToken).ConfigureAwait(false);
        await SeedRolePermissionsAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task SeedPermissionsAsync(CancellationToken cancellationToken)
    {
        var existing = permissionReader.DataSource
            .Select(p => p.NormalizedName)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var name in SystemPermissions.GetAll())
        {
            var normalized = name.ToUpperInvariant();
            if (existing.Contains(normalized))
                continue;

            await permissionRepository.InsertAsync(new Permission(Guid.NewGuid(), name), cancellationToken)
                .ConfigureAwait(false);

            existing.Add(normalized);
        }
    }

    private async Task SeedRolePermissionsAsync(CancellationToken cancellationToken)
    {
        var roles = roleReader.DataSource.ToList();
        var permissions = permissionReader.DataSource.ToList();

        var existingRolePermissions = rolePermissionReader.DataSource
            .Select(rp => new { rp.RoleId, rp.PermissionId })
            .AsEnumerable()
            .Select(rp => (rp.RoleId, rp.PermissionId))
            .ToHashSet();

        foreach (var (roleName, permissionNames) in DefaultRolePermissions())
        {
            var role = roles.FirstOrDefault(r =>
                string.Equals(r.NormalizedName, SystemUserRoleNames.Normalize(roleName), StringComparison.OrdinalIgnoreCase));
            if (role is null)
                continue;

            foreach (var permissionName in permissionNames)
            {
                var normalized = permissionName.ToUpperInvariant();
                var permission = permissions.FirstOrDefault(p =>
                    string.Equals(p.NormalizedName, normalized, StringComparison.OrdinalIgnoreCase));
                if (permission is null)
                    continue;

                if (existingRolePermissions.Contains((role.Id, permission.Id)))
                    continue;

                await rolePermissionRepository.InsertAsync(
                    new RolePermission(role.Id, permission.Id),
                    cancellationToken).ConfigureAwait(false);

                existingRolePermissions.Add((role.Id, permission.Id));
            }
        }
    }

    private static IEnumerable<(string Role, string[] Permissions)> DefaultRolePermissions()
    {
        yield return (SystemUserRoleNames.SalesStaff,
        [
            SystemPermissions.Catalog.CategoriesView,
            SystemPermissions.Catalog.ProductsView,
            SystemPermissions.Orders.View,
            SystemPermissions.Orders.Create,
            SystemPermissions.Orders.Edit,
            SystemPermissions.Orders.FastSale,
            SystemPermissions.DeliveryNotes.View,
            SystemPermissions.DirectShip.View,
            SystemPermissions.Inventory.View,
            SystemPermissions.Customers.View,
            SystemPermissions.Customers.Manage,
            SystemPermissions.CustomerReturns.Manage,
            SystemPermissions.Debts.CustomerDebtsView,
        ]);

        yield return (SystemUserRoleNames.WarehouseManager,
        [
            SystemPermissions.Catalog.ProductsView,
            SystemPermissions.Catalog.VendorsView,
            SystemPermissions.Orders.View,
            SystemPermissions.DeliveryNotes.View,
            SystemPermissions.DeliveryNotes.Manage,
            SystemPermissions.DeliveryRuns.View,
            SystemPermissions.DeliveryRuns.Manage,
            SystemPermissions.DirectShip.View,
            SystemPermissions.DirectShip.Manage,
            SystemPermissions.PurchaseOrders.View,
            SystemPermissions.PurchaseOrders.Create,
            SystemPermissions.GoodsReceipts.Manage,
            SystemPermissions.VendorReturns.Manage,
            SystemPermissions.Inventory.View,
            SystemPermissions.Inventory.Adjust,
            SystemPermissions.Inventory.Transfer,
            SystemPermissions.Inventory.ViewCostDetails,
            SystemPermissions.Inventory.Preparation,
            SystemPermissions.CustomerReturns.Manage,
            SystemPermissions.Debts.VendorDebtsView,
            SystemPermissions.Finance.ReportsDirectShip,
        ]);

        yield return (SystemUserRoleNames.DeliveryStaff,
        [
            SystemPermissions.Orders.View,
            SystemPermissions.DeliveryNotes.View,
            SystemPermissions.DeliveryRuns.View,
            SystemPermissions.DeliveryRuns.MobileAccess,
        ]);

        yield return (SystemUserRoleNames.Cashier,
        [
            SystemPermissions.Orders.View,
            SystemPermissions.Orders.FastSale,
            SystemPermissions.DeliveryNotes.View,
            SystemPermissions.DeliveryRuns.View,
            SystemPermissions.DeliveryRuns.ConfirmCashHandover,
            SystemPermissions.PurchaseOrders.View,
            SystemPermissions.Customers.View,
            SystemPermissions.Debts.CustomerDebtsView,
            SystemPermissions.Debts.CustomerDebtsRecordPayment,
            SystemPermissions.Debts.CustomerRefundsManage,
            SystemPermissions.Debts.VendorDebtsView,
            SystemPermissions.Debts.VendorDebtsRecordPayment,
            SystemPermissions.Finance.ExpensesView,
            SystemPermissions.Finance.ExpensesManage,
            SystemPermissions.Finance.Accounting,
            SystemPermissions.Finance.ReportsFinancial,
        ]);
    }
}
