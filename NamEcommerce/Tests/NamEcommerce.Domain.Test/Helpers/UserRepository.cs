using NamEcommerce.Domain.Entities.Users;

namespace NamEcommerce.Domain.Test.Helpers;

public static class UserRepository
{
    public static Mock<IRepository<User>> SetUsernameExists(string username, Guid id)
        => Repository.Create<User>().WhenCall(r => r.GetAllAsync(), new User(default, username, string.Empty, string.Empty, string.Empty));
}
