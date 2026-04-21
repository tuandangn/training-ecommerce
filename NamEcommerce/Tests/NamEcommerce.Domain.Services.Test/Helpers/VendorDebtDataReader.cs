using NamEcommerce.Domain.Entities.Debts;
using NamEcommerce.Domain.Shared.Common;

namespace NamEcommerce.Domain.Services.Test.Helpers;

public static class VendorDebtDataReader
{
    public static Mock<IEntityDataReader<VendorDebt>> Empty()
        => EntityDataReader.Create<VendorDebt>().WithData(Array.Empty<VendorDebt>());

    public static Mock<IEntityDataReader<VendorDebt>> WithData(params VendorDebt[] debts)
        => EntityDataReader.Create<VendorDebt>().WithData(debts);

    public static Mock<IEntityDataReader<VendorDebt>> NotFound(Guid id)
        => EntityDataReader.Create<VendorDebt>().WhenCall(reader => reader.GetByIdAsync(id), (VendorDebt?)null);

    public static Mock<IEntityDataReader<VendorDebt>> DebtById(Guid id, VendorDebt debt)
        => EntityDataReader.Create<VendorDebt>().WhenCall(reader => reader.GetByIdAsync(id), debt);
}
