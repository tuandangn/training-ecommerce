using NamEcommerce.Domain.Entities.Debts;

namespace NamEcommerce.Domain.Services.Test.Helpers;

public static class VendorPaymentRepository
{
    public static Mock<IRepository<VendorPayment>> InsertWillReturn(VendorPayment @return)
        => Repository.Create<VendorPayment>().WhenCall(r =>
            r.InsertAsync(It.IsAny<VendorPayment>())
        , @return);

    public static Mock<IRepository<VendorPayment>> InsertAnyWillReturn(VendorPayment @return)
        => Repository.Create<VendorPayment>().WhenCall(r =>
            r.InsertAsync(It.IsAny<VendorPayment>())
        , @return);

    public static Mock<IRepository<VendorPayment>> UpdateWillReturn(VendorPayment @return)
        => Repository.Create<VendorPayment>().WhenCall(r =>
            r.UpdateAsync(It.Is<VendorPayment>(p => p.Id == @return.Id))
        , @return);
}
