using NamEcommerce.Domain.Entities.Users;
using NamEcommerce.Domain.Shared.Services;

namespace NamEcommerce.Domain.Services.Test.Helpers;

public static class UserDataReader
{
    public static Mock<IEntityDataReader<User>> HasOne(User user)
        => EntityDataReader.Create<User>().WithData(user);

    public static Mock<IEntityDataReader<User>> SetUsernameExists(string username, Guid id)
        => EntityDataReader.Create<User>().WhenCall(r => r.GetAllAsync(), new User(id, username, string.Empty, string.Empty, string.Empty, string.Empty));

    public static Mock<IEntityDataReader<User>> Empty()
        => EntityDataReader.Create<User>().WithData(Array.Empty<User>());
}
