using NamEcommerce.Domain.Entities.PurchaseOrders;
using NamEcommerce.Domain.Entities.Users;
using NamEcommerce.Domain.Shared.Common;

namespace NamEcommerce.Domain.Services.Test.Helpers;

public static class UserDataReader
{
    public static Mock<IEntityDataReader<User>> HasOne(User user)
        => EntityDataReader.Create<User>().WithData(user);

    public static Mock<IEntityDataReader<User>> SetUsernameExists(string username, out Guid id)
    {
        var user = new User(username, string.Empty, string.Empty);
        id = user.Id;
        return EntityDataReader.Create<User>().WhenCall(r => r.GetAllAsync(), user);
    }

    public static Mock<IEntityDataReader<User>> Empty()
        => EntityDataReader.Create<User>().WithData(Array.Empty<User>());

    public static Mock<IEntityDataReader<User>> NotFound(Guid id)
        => EntityDataReader.Create<User>().WhenCall(reader => reader.GetByIdAsync(id), (User?)null);

    public static Mock<IEntityDataReader<User>> UserById(Guid id, User user)
        => EntityDataReader.Create<User>().WhenCall(reader => reader.GetByIdAsync(id), user);
}
