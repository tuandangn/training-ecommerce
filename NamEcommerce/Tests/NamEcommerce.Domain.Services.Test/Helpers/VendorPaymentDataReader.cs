using NamEcommerce.Domain.Entities.Debts;
using NamEcommerce.Domain.Shared.Common;

namespace NamEcommerce.Domain.Services.Test.Helpers;

public static class VendorPaymentDataReader
{
    public static Mock<IEntityDataReader<VendorPayment>> Empty()
        => EntityDataReader.Create<VendorPayment>().WithData(Array.Empty<VendorPayment>());

    public static Mock<IEntityDataReader<VendorPayment>> WithData(params VendorPayment[] payments)
        => EntityDataReader.Create<VendorPayment>().WithData(payments);

    public static Mock<IEntityDataReader<VendorPayment>> NotFound(Guid id)
        => EntityDataReader.Create<VendorPayment>().WhenCall(reader => reader.GetByIdAsync(id), (VendorPayment?)null);

    public static Mock<IEntityDataReader<VendorPayment>> PaymentById(Guid id, VendorPayment payment)
        => EntityDataReader.Create<VendorPayment>().WhenCall(reader => reader.GetByIdAsync(id), payment);
}
