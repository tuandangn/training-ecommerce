namespace NamEcommerce.Domain.Test.Helpers;

public static class CategoryRepository
{
    public static Mock<IRepository<Category>> SetData(params Category[] categories)
        => Repository.Create<Category>().WhenCall(r => r.GetAllAsync(), categories);

    public static Mock<IRepository<Category>> SetNameExists(string name, int id)
        => Repository.Create<Category>().WhenCall(r => r.GetAllAsync(), new Category(id, name));
    public static Mock<IRepository<Category>> SetNameExists(this Mock<IRepository<Category>> mock, string name, int id)
        => mock.WhenCall(r => r.GetAllAsync(), new Category(id, name));

    public static Mock<IRepository<Category>> CreateCategoryWillReturns(Category category, Category @return)
        => Repository.Create<Category>().WhenCall(r => r.InsertAsync(It.Is<Category>(value => value == category)), @return);

    public static Mock<IRepository<Category>> NotFound(int id)
        => Repository.Create<Category>().WhenCall(r => r.GetByIdAsync(id), (Category?)null);
    public static Mock<IRepository<Category>> NotFound(this Mock<IRepository<Category>> mock, int id)
        => mock.WhenCall(r => r.GetByIdAsync(id), (Category?)null);

    public static Mock<IRepository<Category>> CategoryById(int id, Category @return)
        => Repository.Create<Category>().WhenCall(r => r.GetByIdAsync(id), @return);
    public static Mock<IRepository<Category>> CategoryById(this Mock<IRepository<Category>> mock, int id, Category @return)
        => mock.WhenCall(r => r.GetByIdAsync(id), @return);

    public static Mock<IRepository<Category>> UpdateCategoryWillReturns(this Mock<IRepository<Category>> mock, Category category, Category @return) 
        => mock.WhenCall(r => r.UpdateAsync(category), @return);
}
