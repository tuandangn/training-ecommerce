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
            CanViewDashboard = await CanAsync(user, SystemPermissions.Dashboard.System),

            CanViewOrders = await CanAsync(user, SystemPermissions.Orders.View),
            CanViewOrderFulfillmentSchedule = await CanAsync(user, SystemPermissions.Orders.View),
            CanUseFastSale = await CanAsync(user, SystemPermissions.Orders.QuickCreate),
            CanUsePreparation = await CanAsync(user, SystemPermissions.Inventory.Preparation),
            CanManageCustomerReturns = await CanAsync(user, SystemPermissions.CustomerReturns.Manage),
            CanViewCustomers = await CanAsync(user, SystemPermissions.Customers.View),

            CanViewPurchaseOrders = await CanAsync(user, SystemPermissions.PurchaseOrders.View),
            CanCreatePurchaseOrders = await CanAsync(user, SystemPermissions.PurchaseOrders.Create),
            CanViewDirectShip = await CanAsync(user, SystemPermissions.DirectShip.View),
            CanManageVendorReturns = await CanAsync(user, SystemPermissions.VendorReturns.Manage),
            CanViewVendors = await CanAsync(user, SystemPermissions.Catalog.VendorsView),

            CanViewInventory = await CanAsync(user, SystemPermissions.Inventory.View),
            CanViewDeliveryNotes = await CanAsync(user, SystemPermissions.DeliveryNotes.View),
            CanViewDeliveryRuns = await CanAsync(user, SystemPermissions.DeliveryRuns.View),
            CanManageGoodsReceipts = await CanAsync(user, SystemPermissions.GoodsReceipts.Manage),
            CanAdjustInventory = await CanAsync(user, SystemPermissions.Inventory.Adjust),

            CanUseDeliveryMobile = await CanAsync(user, SystemPermissions.DeliveryRuns.MobileAccess),

            CanViewCustomerDebts = await CanAsync(user, SystemPermissions.Debts.CustomerDebtsView),
            CanViewVendorDebts = await CanAsync(user, SystemPermissions.Debts.VendorDebtsView),
            CanManageCustomerRefunds = await CanAsync(user, SystemPermissions.Debts.CustomerRefundsManage),
            CanViewFinancialReports = await CanAsync(user, SystemPermissions.Finance.ReportsFinancial),
            CanViewDirectShipReports = await CanAsync(user, SystemPermissions.Finance.ReportsDirectShip),
            CanViewExpenses = await CanAsync(user, SystemPermissions.Finance.ExpensesView),

            CanViewProducts = await CanAsync(user, SystemPermissions.Catalog.ProductsView),
            CanViewCategories = await CanAsync(user, SystemPermissions.Catalog.CategoriesView),
            CanManageWarehouses = await CanAsync(user, SystemPermissions.Warehouses.Manage),
            CanManageUnitMeasurements = await CanAsync(user, SystemPermissions.Catalog.UnitMeasurementsManage),

            CanManageUserRoles = await CanAsync(user, AuthorizationPolicyNames.ManageUserRoles),
            CanManageCustomerPortal = await CanAsync(user, AuthorizationPolicyNames.ManageUserRoles),
            CanManageUsers = await CanAsync(user, SystemPermissions.Users.Manage),

            CanViewAccounting = await CanAsync(user, SystemPermissions.Finance.Accounting),
        };

        return View(model);
    }

    private async Task<bool> CanAsync(ClaimsPrincipal user, string policy)
    {
        var result = await authorizationService.AuthorizeAsync(user, policy);
        return result.Succeeded;
    }
}
