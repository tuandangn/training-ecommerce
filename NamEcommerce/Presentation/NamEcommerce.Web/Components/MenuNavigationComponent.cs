using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NamEcommerce.Web.Constants;
using NamEcommerce.Web.Contracts.Security;
using NamEcommerce.Web.Models.Common;
using System.Security.Claims;

namespace NamEcommerce.Web.Components;

public sealed class MenuNavigationComponent(IAuthorizationService authorizationService) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync()
    {
        var user = HttpContext.User;
        var model = new MenuNavigationModel
        {
            CanViewDashboard = user.Identity?.IsAuthenticated == true,

            CanViewOrders = await CanAsync(user, SystemPermissions.Orders.View).ConfigureAwait(false),
            CanUseFastSale = await CanAsync(user, SystemPermissions.Orders.FastSale).ConfigureAwait(false),
            CanUsePreparation = await CanAsync(user, SystemPermissions.Inventory.Preparation).ConfigureAwait(false),
            CanManageCustomerReturns = await CanAsync(user, SystemPermissions.CustomerReturns.Manage).ConfigureAwait(false),
            CanViewCustomers = await CanAsync(user, SystemPermissions.Customers.View).ConfigureAwait(false),

            CanViewPurchaseOrders = await CanAsync(user, SystemPermissions.PurchaseOrders.View).ConfigureAwait(false),
            CanCreatePurchaseOrders = await CanAsync(user, SystemPermissions.PurchaseOrders.Create).ConfigureAwait(false),
            CanViewDirectShip = await CanAsync(user, SystemPermissions.DirectShip.View).ConfigureAwait(false),
            CanManageVendorReturns = await CanAsync(user, SystemPermissions.VendorReturns.Manage).ConfigureAwait(false),
            CanViewVendors = await CanAsync(user, SystemPermissions.Catalog.VendorsView).ConfigureAwait(false),

            CanViewInventory = await CanAsync(user, SystemPermissions.Inventory.View).ConfigureAwait(false),
            CanViewDeliveryNotes = await CanAsync(user, SystemPermissions.DeliveryNotes.View).ConfigureAwait(false),
            CanViewDeliveryRuns = await CanAsync(user, SystemPermissions.DeliveryRuns.View).ConfigureAwait(false),
            CanManageGoodsReceipts = await CanAsync(user, SystemPermissions.GoodsReceipts.Manage).ConfigureAwait(false),
            CanAdjustInventory = await CanAsync(user, SystemPermissions.Inventory.Adjust).ConfigureAwait(false),

            CanUseDeliveryMobile = await CanAsync(user, SystemPermissions.DeliveryRuns.MobileAccess).ConfigureAwait(false),

            CanViewCustomerDebts = await CanAsync(user, SystemPermissions.Debts.CustomerDebtsView).ConfigureAwait(false),
            CanViewVendorDebts = await CanAsync(user, SystemPermissions.Debts.VendorDebtsView).ConfigureAwait(false),
            CanManageCustomerRefunds = await CanAsync(user, SystemPermissions.Debts.CustomerRefundsManage).ConfigureAwait(false),
            CanViewFinancialReports = await CanAsync(user, SystemPermissions.Finance.ReportsFinancial).ConfigureAwait(false),
            CanViewDirectShipReports = await CanAsync(user, SystemPermissions.Finance.ReportsDirectShip).ConfigureAwait(false),
            CanViewExpenses = await CanAsync(user, SystemPermissions.Finance.ExpensesView).ConfigureAwait(false),

            CanViewProducts = await CanAsync(user, SystemPermissions.Catalog.ProductsView).ConfigureAwait(false),
            CanViewCategories = await CanAsync(user, SystemPermissions.Catalog.CategoriesView).ConfigureAwait(false),
            CanManageWarehouses = await CanAsync(user, SystemPermissions.Warehouses.Manage).ConfigureAwait(false),
            CanManageUnitMeasurements = await CanAsync(user, SystemPermissions.Catalog.UnitMeasurementsManage).ConfigureAwait(false),

            CanManageUserRoles = await CanAsync(user, AuthorizationPolicyNames.ManageUserRoles).ConfigureAwait(false),
            CanManageCustomerPortal = await CanAsync(user, AuthorizationPolicyNames.ManageUserRoles).ConfigureAwait(false),
            CanManageUsers = await CanAsync(user, SystemPermissions.Users.Manage).ConfigureAwait(false),

            CanViewAccounting = await CanAsync(user, SystemPermissions.Finance.Accounting).ConfigureAwait(false),
        };

        return View(model);
    }

    private async Task<bool> CanAsync(ClaimsPrincipal user, string policy)
    {
        var result = await authorizationService.AuthorizeAsync(user, policy).ConfigureAwait(false);
        return result.Succeeded;
    }
}
