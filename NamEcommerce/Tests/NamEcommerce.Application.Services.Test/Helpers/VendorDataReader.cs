using NamEcommerce.Domain.Entities.Catalog;
using NamEcommerce.Domain.Shared.Services;
using NamEcommerce.TestHelper;

namespace NamEcommerce.Application.Services.Test.Helpers;

public static class VendorDataReader
{
    public static Mock<IEntityDataReader<Vendor>> SetData(params Vendor[] vendors)
        => EntityDataReader.Create<Vendor>()
            .WithData(vendors);

    public static Mock<IEntityDataReader<Vendor>> NotFound(Guid id)
        => EntityDataReader.Create<Vendor>()
            .WhenCall(reader => reader.GetByIdAsync(id, default), (Vendor)null!);

    public static Mock<IEntityDataReader<Vendor>> AllVendors(params Vendor[] vendors)
        => EntityDataReader.Create<Vendor>()
            .WhenCall(reader => reader.GetAllAsync(), vendors);

    public static Mock<IEntityDataReader<Vendor>> VendorById(Guid id, Vendor vendor)
        => EntityDataReader.Create<Vendor>()
            .WhenCall(reader => reader.GetByIdAsync(id, default), vendor);

    public static Mock<IEntityDataReader<Vendor>> VendorsByIds(Guid[] ids, params Vendor[] vendors)
        => EntityDataReader.Create<Vendor>()
            .WhenCall(reader => reader.GetByIdsAsync(ids), vendors);
}
