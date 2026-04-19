using NamEcommerce.Domain.Entities.Customers;
using NamEcommerce.Domain.Shared.Common;

namespace NamEcommerce.Domain.Services.Test.Helpers;

public static class CustomerDataReader
{
    public static Mock<IEntityDataReader<Customer>> Empty()
        => EntityDataReader.Create<Customer>().WithData(Array.Empty<Customer>());

    public static Mock<IEntityDataReader<Customer>> WithData(params Customer[] customers)
        => EntityDataReader.Create<Customer>().WithData(customers);

    public static Mock<IEntityDataReader<Customer>> HasOne(Customer customer)
        => EntityDataReader.Create<Customer>().WithData(customer);

    public static Mock<IEntityDataReader<Customer>> NotFound(Guid id)
        => EntityDataReader.Create<Customer>().WhenCall(reader => reader.GetByIdAsync(id), (Customer?)null);

    public static Mock<IEntityDataReader<Customer>> CustomerById(Guid id, Customer customer)
        => EntityDataReader.Create<Customer>().WhenCall(reader => reader.GetByIdAsync(id), customer);

    public static Mock<IEntityDataReader<Customer>> CustomerById(this Mock<IEntityDataReader<Customer>> mock, Guid id, Customer customer)
        => mock.WhenCall(reader => reader.GetByIdAsync(id), customer);
}
