namespace NamEcommerce.Domain.Services.Test.Helpers;

public static class VendorRepository
{
    public static Mock<IRepository<Vendor>> CreateVendorWillReturns(Vendor @return)
        //*TODO* check inserting data
        => Repository.Create<Vendor>().WhenCall(r => 
            r.InsertAsync(It.Is<Vendor>(entity => 
                entity.Name == @return.Name && entity.PhoneNumber == @return.PhoneNumber 
                && entity.Address == @return.Address && entity.DisplayOrder == @return.DisplayOrder))
        , @return);

    public static Mock<IRepository<Vendor>> NotFound(Guid id)
        => Repository.Create<Vendor>().WhenCall(r => r.GetByIdAsync(id, default), (Vendor?)null);
    public static Mock<IRepository<Vendor>> NotFound(this Mock<IRepository<Vendor>> mock, Guid id)
        => mock.WhenCall(r => r.GetByIdAsync(id, default), (Vendor?)null);

    public static Mock<IRepository<Vendor>> VendorById(Guid id, Vendor @return)
        => Repository.Create<Vendor>().WhenCall(r => r.GetByIdAsync(id, default), @return);
    public static Mock<IRepository<Vendor>> VendorById(this Mock<IRepository<Vendor>> mock, Guid id, Vendor @return)
        => mock.WhenCall(r => r.GetByIdAsync(id, default), @return);

    public static Mock<IRepository<Vendor>> CanDeleteVendor(Vendor vendor)
        => Repository.Create<Vendor>().CanCall(r => r.DeleteAsync(vendor));
}
