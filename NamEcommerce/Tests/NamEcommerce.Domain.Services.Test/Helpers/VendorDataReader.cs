using NamEcommerce.Domain.Shared.Common;

namespace NamEcommerce.Domain.Services.Test.Helpers;

public static class VendorDataReader
{
    public static Mock<IEntityDataReader<Vendor>> Empty()
        => EntityDataReader.Create<Vendor>().WithData(Array.Empty<Vendor>());

    public static Mock<IEntityDataReader<Vendor>> WithData(params Vendor[] vendors)
        => EntityDataReader.Create<Vendor>().WithData(vendors);
    public static Mock<IEntityDataReader<Vendor>> HasOne(Vendor vendor)
        => EntityDataReader.Create<Vendor>().WithData(vendor);

    public static Mock<IEntityDataReader<Vendor>> NotFound(Guid id)
        => EntityDataReader.Create<Vendor>().WhenCall(reader => reader.GetByIdAsync(id), (Vendor?)null);

    public static Mock<IEntityDataReader<Vendor>> VendorById(Guid id, Vendor vendor)
        => EntityDataReader.Create<Vendor>().WhenCall(reader => reader.GetByIdAsync(id), vendor);
    public static Mock<IEntityDataReader<Vendor>> VendorById(this Mock<IEntityDataReader<Vendor>> mock, Guid id, Vendor vendor)
        => mock.WhenCall(reader => reader.GetByIdAsync(id), vendor);
}
