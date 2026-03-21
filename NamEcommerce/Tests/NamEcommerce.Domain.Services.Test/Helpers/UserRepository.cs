using NamEcommerce.Domain.Entities.Users;

namespace NamEcommerce.Domain.Services.Test.Helpers;

public static class UserRepository
{
    public static Mock<IRepository<User>> SetUsernameExists(string username, Guid id)
        => Repository.Create<User>().WhenCall(r => r.GetAllAsync(), new User(default, username, string.Empty, string.Empty, string.Empty, string.Empty));

    public static Mock<IRepository<User>> CreateUserWillReturns(User user, User @return)
        //*TODO* check inserting data
        => Repository.Create<User>().WhenCall(r => r.InsertAsync(It.IsAny<User>(), default), @return);
}
