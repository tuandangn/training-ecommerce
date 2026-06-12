namespace NamEcommerce.Domain.Shared.Enums.Debts;

public enum CustomerLedgerEntryType
{
    DeliveryNoteCharge = 10,
    OpeningBalance = 20,
    Payment = 30,
    ReturnCredit = 40,
    RefundPayout = 50,
    Correction = 60,
    Surcharge = 70
}

public enum CustomerLedgerReferenceType
{
    None = 0,
    DeliveryNote = 10,
    CustomerPayment = 20,
    CustomerReturn = 30,
    CustomerRefund = 40
}
