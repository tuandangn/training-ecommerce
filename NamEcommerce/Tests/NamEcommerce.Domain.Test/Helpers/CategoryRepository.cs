namespace NamEcommerce.Domain.Test.Helpers;

public sealed class CategoryRepository
{
    public static Mock<IRepository<Category>> SetNameExists(string name)
        => Repository.Create<Category>().WhenCall(r => r.GetAllAsync(), new Category(default, name));

    public static Mock<IRepository<Category>> Create(Category category, Category @return)
        => Repository.Create<Category>().WhenCall(r => r.InsertAsync(It.Is<Category>(value => value == category)), @return);
}
