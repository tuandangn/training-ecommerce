namespace NamEcommerce.Web.Models.Common;

public sealed record MenuNavigationModel
{
    public required bool CanViewDashboard { get; init; }

    public required bool CanViewOrders { get; init; }
    public required bool CanViewOrderFulfillmentSchedule { get; init; }
    public required bool CanUseFastSale { get; init; }
    public required bool CanUsePreparation { get; init; }
    public required bool CanManageCustomerReturns { get; init; }
    public required bool CanViewCustomers { get; init; }

    public required bool CanViewPurchaseOrders { get; init; }
    public required bool CanCreatePurchaseOrders { get; init; }
    public required bool CanViewDirectShip { get; init; }
    public required bool CanManageVendorReturns { get; init; }
    public required bool CanViewVendors { get; init; }

    public required bool CanViewInventory { get; init; }
    public required bool CanViewDeliveryNotes { get; init; }
    public required bool CanViewDeliveryRuns { get; init; }
    public required bool CanManageGoodsReceipts { get; init; }
    public required bool CanAdjustInventory { get; init; }

    public required bool CanUseDeliveryMobile { get; init; }

    public required bool CanViewCustomerDebts { get; init; }
    public required bool CanViewVendorDebts { get; init; }
    public required bool CanManageCustomerRefunds { get; init; }
    public required bool CanViewFinancialReports { get; init; }
    public required bool CanViewDirectShipReports { get; init; }
    public required bool CanViewExpenses { get; init; }

    public required bool CanViewProducts { get; init; }
    public required bool CanViewCategories { get; init; }
    public required bool CanManageWarehouses { get; init; }
    public required bool CanManageUnitMeasurements { get; init; }

    public required bool CanManageCustomerPortal { get; init; }
    public required bool CanManageUserRoles { get; init; }
    public required bool CanManageUsers { get; init; }

    public required bool CanViewAccounting { get; init; }
}
