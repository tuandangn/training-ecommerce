namespace NamEcommerce.Web.Contracts.Security;

public static class SystemPermissions
{
    public static class Dashboard
    {
        public const string System = "Dashboard.System.View";
    }

    public static class Catalog
    {
        public const string CategoriesView = "Catalog.Categories.View";
        public const string CategoriesManage = "Catalog.Categories.Manage";
        public const string ProductsView = "Catalog.Products.View";
        public const string ProductsManage = "Catalog.Products.Manage";
        public const string VendorsView = "Catalog.Vendors.View";
        public const string VendorsManage = "Catalog.Vendors.Manage";
        public const string UnitMeasurementsManage = "Catalog.UnitMeasurements.Manage";
    }

    public static class Orders
    {
        public const string View = "Orders.View";
        public const string Create = "Orders.Create";
        public const string Edit = "Orders.Edit";
        public const string Cancel = "Orders.Cancel";
        public const string QuickCreate = "Orders.QuickCreate";
    }

    public static class DeliveryNotes
    {
        public const string View = "DeliveryNotes.View";
        public const string Create = "DeliveryNotes.Create";
        public const string Approve = "DeliveryNotes.Approve";
        public const string Manage = "DeliveryNotes.Manage";
    }

    public static class DeliveryRuns
    {
        public const string View = "DeliveryRuns.View";
        public const string Manage = "DeliveryRuns.Manage";
        public const string ConfirmCashHandover = "DeliveryRuns.ConfirmCashHandover";
        public const string MobileAccess = "DeliveryRuns.MobileAccess";
    }

    public static class DirectShip
    {
        public const string View = "DirectShip.View";
        public const string Manage = "DirectShip.Manage";
    }

    public static class PurchaseOrders
    {
        public const string View = "PurchaseOrders.View";
        public const string Create = "PurchaseOrders.Create";
        public const string Edit = "PurchaseOrders.Edit";
        public const string Approve = "PurchaseOrders.Approve";
        public const string Cancel = "PurchaseOrders.Cancel";
        public const string QuickCreate = "PurchaseOrders.QuickCreate";
    }

    public static class GoodsReceipts
    {
        public const string Manage = "GoodsReceipts.Manage";
    }

    public static class VendorReturns
    {
        public const string Manage = "VendorReturns.Manage";
    }

    public static class Inventory
    {
        public const string View = "Inventory.View";
        public const string Adjust = "Inventory.Adjust";
        public const string Transfer = "Inventory.Transfer";
        public const string ViewCostDetails = "Inventory.ViewCostDetails";
        public const string Preparation = "Inventory.Preparation";
    }

    public static class Warehouses
    {
        public const string Manage = "Warehouses.Manage";
    }

    public static class Customers
    {
        public const string View = "Customers.View";
        public const string Manage = "Customers.Manage";
    }

    public static class CustomerReturns
    {
        public const string Manage = "CustomerReturns.Manage";
    }

    public static class Debts
    {
        public const string CustomerDebtsView = "Debts.CustomerDebts.View";
        public const string CustomerDebtsRecordPayment = "Debts.CustomerDebts.RecordPayment";
        public const string CustomerRefundsManage = "Debts.CustomerRefunds.Manage";
        public const string VendorDebtsView = "Debts.VendorDebts.View";
        public const string VendorDebtsRecordPayment = "Debts.VendorDebts.RecordPayment";
        public const string VendorRefundsManage = "Debts.VendorRefunds.Manage";
    }

    public static class Finance
    {
        public const string ExpensesView = "Finance.Expenses.View";
        public const string ExpensesManage = "Finance.Expenses.Manage";
        public const string Accounting = "Finance.Accounting";
        public const string ReportsFinancial = "Finance.Reports.Financial";
        public const string ReportsDirectShip = "Finance.Reports.DirectShip";
    }

    public static class Users
    {
        public const string ManageRoles = "Users.ManageRoles";
        public const string Manage = "Users.Manage";
    }

    public static IReadOnlyList<string> GetAll() =>
    [
        Dashboard.System,

        Catalog.CategoriesView, Catalog.CategoriesManage,
        Catalog.ProductsView, Catalog.ProductsManage,
        Catalog.VendorsView, Catalog.VendorsManage,
        Catalog.UnitMeasurementsManage,

        Orders.View, Orders.Create, Orders.Edit, Orders.Cancel, Orders.QuickCreate,

        DeliveryNotes.View, DeliveryNotes.Manage, DeliveryNotes.Approve,
        DeliveryRuns.View, DeliveryRuns.Manage, DeliveryRuns.ConfirmCashHandover, DeliveryRuns.MobileAccess,
        DirectShip.View, DirectShip.Manage,

        PurchaseOrders.View, PurchaseOrders.Create, PurchaseOrders.Edit, PurchaseOrders.Cancel, PurchaseOrders.Approve, PurchaseOrders.QuickCreate,
        GoodsReceipts.Manage,
        VendorReturns.Manage,

        Inventory.View, Inventory.Adjust, Inventory.Transfer, Inventory.ViewCostDetails, Inventory.Preparation,
        Warehouses.Manage,

        Customers.View, Customers.Manage,
        CustomerReturns.Manage,

        Debts.CustomerDebtsView, Debts.CustomerDebtsRecordPayment,
        Debts.CustomerRefundsManage,
        Debts.VendorDebtsView, Debts.VendorDebtsRecordPayment,
        Debts.VendorRefundsManage,

        Finance.ExpensesView, Finance.ExpensesManage,
        Finance.Accounting,
        Finance.ReportsFinancial, Finance.ReportsDirectShip,

        Users.ManageRoles,
        Users.Manage,
    ];
}
