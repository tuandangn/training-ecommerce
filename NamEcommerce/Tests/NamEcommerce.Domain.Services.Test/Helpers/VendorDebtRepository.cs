using NamEcommerce.Domain.Entities.Debts;

namespace NamEcommerce.Domain.Services.Test.Helpers;

public static class VendorDebtRepository
{
    public static Mock<IRepository<VendorDebt>> InsertWillReturn(VendorDebt @return)
        => Repository.Create<VendorDebt>().WhenCall(r =>
            r.InsertAsync(It.Is<VendorDebt>(d =>
                d.VendorId == @return.VendorId
                && d.PurchaseOrderId == @return.PurchaseOrderId
                && d.TotalAmount == @return.TotalAmount))
        , @return);

    public static Mock<IRepository<VendorDebt>> UpdateWillReturn(VendorDebt @return)
        => Repository.Create<VendorDebt>().WhenCall(r =>
            r.UpdateAsync(It.Is<VendorDebt>(d => d.Id == @return.Id))
        , @return);

    public static Mock<IRepository<VendorDebt>> DebtById(Guid id, VendorDebt @return)
        => Repository.Create<VendorDebt>().WhenCall(r => r.GetByIdAsync(id, default), @return);

    public static Mock<IRepository<VendorDebt>> NotFound(Guid id)
        => Repository.Create<VendorDebt>().WhenCall(r => r.GetByIdAsync(id, default), (VendorDebt?)null);
}
