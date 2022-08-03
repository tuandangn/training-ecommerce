namespace NamEcommerce.Domain.Test.Helpers;

public static class CategoryRepository
{
    public static Mock<IRepository<Category>> SetData(params Category[] categories)
        => Repository.Create<Category>().WhenCall(r => r.GetAllAsync(), categories);

    public static Mock<IRepository<Category>> SetNameExists(string name, Guid id)
        => Repository.Create<Category>().WhenCall(r => r.GetAllAsync(), new Category(id, name));
    public static Mock<IRepository<Category>> SetNameExists(this Mock<IRepository<Category>> mock, string name, Guid id)
        => mock.WhenCall(r => r.GetAllAsync(), new Category(id, name));

    public static Mock<IRepository<Category>> CreateCategoryWillReturns(Category category, Category @return)
        //*TODO* check inserting data
        => Repository.Create<Category>().WhenCall(r => r.InsertAsync(It.IsAny<Category>(), default), @return);

    public static Mock<IRepository<Category>> NotFound(Guid id)
        => Repository.Create<Category>().WhenCall(r => r.GetByIdAsync(id, default), (Category?)null);
    public static Mock<IRepository<Category>> NotFound(this Mock<IRepository<Category>> mock, Guid id)
        => mock.WhenCall(r => r.GetByIdAsync(id, default), (Category?)null);

    public static Mock<IRepository<Category>> CategoryById(Guid id, Category @return)
        => Repository.Create<Category>().WhenCall(r => r.GetByIdAsync(id, default), @return);
    public static Mock<IRepository<Category>> CategoryById(this Mock<IRepository<Category>> mock, Guid id, Category @return)
        => mock.WhenCall(r => r.GetByIdAsync(id, default), @return);

    public static Mock<IRepository<Category>> UpdateCategoryWillReturns(this Mock<IRepository<Category>> mock, Category category, Category @return) 
        => mock.WhenCall(r => r.UpdateAsync(category, default), @return);
}
