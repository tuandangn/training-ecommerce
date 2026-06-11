namespace NamEcommerce.Domain.Shared.Enums.Debts;

public enum VendorLedgerEntryType
{
    GoodsReceiptCharge = 10,
    OpeningBalance = 20,
    Payment = 30,
    ReturnCredit = 40,
    RefundReceipt = 50,
    Correction = 60
}

public enum VendorLedgerReferenceType
{
    None = 0,
    GoodsReceipt = 10,
    VendorPayment = 20,
    VendorReturn = 30,
    VendorRefund = 40
}
