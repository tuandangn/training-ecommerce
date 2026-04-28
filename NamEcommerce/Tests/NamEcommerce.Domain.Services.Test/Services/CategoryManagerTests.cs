using NamEcommerce.Domain.Services.Extensions;
using NamEcommerce.Domain.Services.Test.Helpers;
using NamEcommerce.Domain.Shared.Events.Catalog;
using System.Linq.Expressions;

namespace NamEcommerce.Domain.Services.Test.Services;

public sealed class CategoryManagerTests
{
    #region CreateCategoryAsync

    [Fact]
    public async Task CreateCategoryAsync_DtoIsNull_ThrowsArgumentNullException()
    {
        var categoryManager = new CategoryManager(null!, null!);

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            categoryManager.CreateCategoryAsync(null!)
        );
    }

    [Fact]
    public async Task CreateCategoryAsync_DataIsInvalid_ThrowsCategoryDataIsInvalidException()
    {
        var invalidCategory = new CreateCategoryDto
        {
            Name = string.Empty,
            ParentId = null
        };
        var categoryManager = new CategoryManager(null!, null!);

        await Assert.ThrowsAsync<CategoryDataIsInvalidException>(() =>
            categoryManager.CreateCategoryAsync(invalidCategory)
        );
    }

    [Fact]
    public async Task CreateCategoryAsync_NameIsExists_ThrowsCategoryNameExistsException()
    {
        var testName = "existing-name";
        var categoryDataReaderMock = CategoryDataReader.HasOne(new Category(default, testName));
        var categoryManager = new CategoryManager(null!, categoryDataReaderMock.Object);

        await Assert.ThrowsAsync<CategoryNameExistsException>(() =>
            categoryManager.CreateCategoryAsync(new CreateCategoryDto
            {
                Name = testName,
                ParentId = null
            })
        );
        categoryDataReaderMock.Verify();
    }

    [Fact]
    public async Task CreateCategoryAsync_ParentCategoryNotFound_ThrowArgumentException()
    {
        var notFoundParentId = Guid.NewGuid();
        var dto = new CreateCategoryDto
        {
            Name = "category",
            ParentId = notFoundParentId,
            DisplayOrder = 1
        };
        var categoryDataReaderMock = CategoryDataReader.NotFound(notFoundParentId);
        var categoryManager = new CategoryManager(null!, categoryDataReaderMock.Object);

        await Assert.ThrowsAsync<CategoryIsNotFoundException>(() => categoryManager.CreateCategoryAsync(dto));

        categoryDataReaderMock.Verify();
    }

    [Fact]
    public async Task CreateCategoryAsync_DataIsValid_ReturnsCreatedCategory()
    {
        var category = new Category(Guid.NewGuid(), "name") { DisplayOrder = 1 };
        var returnCategory = new Category(category.Id, category.Name)
        {
            DisplayOrder = category.DisplayOrder
        };
        var categoryRepositoryMock = CategoryRepository.CreateCategoryWillReturns(category, returnCategory);
        var categoryDataReaderStub = CategoryDataReader.Empty();
        var categoryManager = new CategoryManager(categoryRepositoryMock.Object, categoryDataReaderStub.Object);

        var createCategoryResultDto = await categoryManager.CreateCategoryAsync(
            new CreateCategoryDto
            {
                Name = category.Name,
                ParentId = null,
                DisplayOrder = category.DisplayOrder
            });

        Assert.Equal(createCategoryResultDto.CreatedId, returnCategory.Id);
        categoryRepositoryMock.Verify();
        categoryRepositoryMock.Verify(r => r.InsertAsync(It.Is<Category>(c =>
            c.DomainEvents.OfType<CategoryCreated>().Any(ev => ev.Name == category.Name && ev.ParentId == null)
            && c.DomainEvents.Count == 1), default), Times.Once);
        categoryDataReaderStub.Verify();
    }

    #endregion

    #region DoesNameExistAsync

    [Fact]
    public async Task DoesNameExistAsync_NameIsNull_ThrowsArgumentNullException()
    {
        var categoryManager = new CategoryManager(null!, null!);

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            categoryManager.DoesNameExistAsync(null!)
        );
    }

    [Fact]
    public async Task DoesNameExistAsync_NameIsMatchAndCompareIdEquals_ReturnsFalse()
    {
        var hasNameCategoryId = Guid.NewGuid();
        var testName = "test-name-existing";
        var categoryDataReaderMock = CategoryDataReader.HasOne(new Category(hasNameCategoryId, testName));
        var categoryManager = new CategoryManager(null!, categoryDataReaderMock.Object);

        var nameExists = await categoryManager.DoesNameExistAsync(testName, comparesWithCurrentId: hasNameCategoryId);

        Assert.False(nameExists);
        categoryDataReaderMock.Verify();
    }

    [Fact]
    public async Task DoesNameExistAsync_NameIsMatchAndCompareIdIsNotProvided_ReturnsTrue()
    {
        var testName = "test-name-existing";
        var categoryDataReaderMock = CategoryDataReader.HasOne(new Category(default, testName));
        var categoryManager = new CategoryManager(null!, categoryDataReaderMock.Object);

        var nameExists = await categoryManager.DoesNameExistAsync(testName, comparesWithCurrentId: null);

        Assert.True(nameExists);
        categoryDataReaderMock.Verify();
    }

    #endregion

    #region UpdateCategoryAsync

    [Fact]
    public async Task UpdateCategoryAsync_DtoIsNull_ThrowsArgumentNullException()
    {
        var categoryManager = new CategoryManager(null!, null!);

        await Assert.ThrowsAsync<ArgumentNullException>(() => categoryManager.UpdateCategoryAsync(null!));
    }

    [Fact]
    public async Task UpdateCategoryAsync_DataIsInvalid_ThrowsCategoryDataIsInvalidException()
    {
        var categoryManager = new CategoryManager(null!, null!);

        await Assert.ThrowsAsync<CategoryDataIsInvalidException>(() => categoryManager.UpdateCategoryAsync(new UpdateCategoryDto(Guid.NewGuid())
        {
            Name = string.Empty,
            ParentId = null
        }));
    }

    [Fact]
    public async Task UpdateCategoryAsync_CategoryIsNotFound_ThrowsArgumentException()
    {
        var notFoundCategoryId = Guid.NewGuid();
        var categoryDataReaderMock = CategoryDataReader.NotFound(notFoundCategoryId);
        var categoryManager = new CategoryManager(null!, categoryDataReaderMock.Object);

        await Assert.ThrowsAsync<CategoryIsNotFoundException>(()
            => categoryManager.UpdateCategoryAsync(new UpdateCategoryDto(notFoundCategoryId)
            {
                Name = "category",
                ParentId = null
            }));
        categoryDataReaderMock.Verify();
    }

    [Fact]
    public async Task UpdateCategoryAsync_CategoryNameIsExists_ThrowsCategoryNameExistsException()
    {
        var updateCategory = new Category(Guid.NewGuid(), "category");
        var sameNameCategory = new Category(Guid.NewGuid(), updateCategory.Name);
        var categoryDataReaderMock = CategoryDataReader
            .HasOne(sameNameCategory)
            .CategoryById(updateCategory.Id, new Category(updateCategory.Id, "old-category-name"));
        var categoryManager = new CategoryManager(null!, categoryDataReaderMock.Object);

        await Assert.ThrowsAsync<CategoryNameExistsException>(()
            => categoryManager.UpdateCategoryAsync(new UpdateCategoryDto(updateCategory.Id)
            {
                Name = updateCategory.Name,
                ParentId = updateCategory.ParentId,
                DisplayOrder = updateCategory.DisplayOrder
            }));

        categoryDataReaderMock.Verify();
    }

    [Fact]
    public async Task UpdateCategoryAsync_ParentCategoryNotFound_ThrowsArgumentException()
    {
        var notFoundParentId = Guid.NewGuid();
        var dto = new UpdateCategoryDto(Guid.NewGuid())
        {
            Name = "category-name",
            ParentId = notFoundParentId,
            DisplayOrder = 1
        };
        var categoryDataReaderMock = CategoryDataReader
            .CategoryById(dto.Id, new Category(dto.Id, "old-category-name"));
        var categoryManager = new CategoryManager(null!, categoryDataReaderMock.Object);

        await Assert.ThrowsAsync<CategoryIsNotFoundException>(() => categoryManager.UpdateCategoryAsync(dto));

        categoryDataReaderMock.Verify();
    }

    [Fact]
    public async Task UpdateCategoryAsync_UpdateCategory()
    {
        var oldCategory = new Category(Guid.NewGuid(), "old-category-name")
        {
            DisplayOrder = 1
        };
        var updateCategory = new Category(oldCategory.Id, "new-category-name")
        {
            DisplayOrder = 2
        };
        Expression<Func<Category, bool>> isCategoryMatch =
            c => c.Id == updateCategory.Id
                && c.Name == updateCategory.Name
                && c.DisplayOrder == updateCategory.DisplayOrder
                && c.DomainEvents.OfType<CategoryUpdated>().Any(ev => ev.CategoryId == updateCategory.Id);
        var categoryRepositoryMock = Repository.Create<Category>()
            .WhenCall(repository => repository.UpdateAsync(It.Is(isCategoryMatch), default), updateCategory);
        var categoryDataReaderMock = CategoryDataReader.CategoryById(oldCategory.Id, oldCategory);
        var categoryManager = new CategoryManager(categoryRepositoryMock.Object, categoryDataReaderMock.Object);

        var updateCategoryResult = await categoryManager.UpdateCategoryAsync(
            new UpdateCategoryDto(updateCategory.Id)
            {
                Name = updateCategory.Name,
                DisplayOrder = updateCategory.DisplayOrder,
                ParentId = updateCategory.ParentId
            });

        Assert.Equal(updateCategoryResult.Id, updateCategory.Id);
        Assert.Equal(updateCategoryResult.Name, updateCategory.Name);
        Assert.Equal(updateCategoryResult.DisplayOrder, updateCategory.DisplayOrder);
        Assert.Equal(updateCategoryResult.ParentId, updateCategory.ParentId);
        categoryRepositoryMock.Verify();
        categoryDataReaderMock.Verify();
    }

    #endregion

    #region SetParentCategory

    [Fact]
    public async Task SetParentCategory_CategoryIsNotFound_ThrowsArgumentException()
    {
        var notFoundCategoryId = Guid.NewGuid();
        var categoryDataReaderMock = CategoryDataReader.NotFound(notFoundCategoryId);
        var categoryManager = new CategoryManager(null!, categoryDataReaderMock.Object);

        await Assert.ThrowsAsync<CategoryIsNotFoundException>(()
            => categoryManager.SetParentCategoryAsync(notFoundCategoryId, default, default));

        categoryDataReaderMock.Verify(repository => repository.GetByIdAsync(notFoundCategoryId), Times.Once);
    }

    [Fact]
    public async Task SetParentCategory_ParentCategoryIsNotFound_ThrowsArgumentException()
    {
        var categoryId = Guid.NewGuid();
        var notFoundParentCategoryId = Guid.NewGuid();
        var categoryDataReaderMock = CategoryDataReader
            .NotFound(notFoundParentCategoryId)
            .CategoryById(categoryId, new Category(categoryId, "category"));
        var categoryManager = new CategoryManager(null!, categoryDataReaderMock.Object);

        await Assert.ThrowsAsync<CategoryIsNotFoundException>(()
            => categoryManager.SetParentCategoryAsync(categoryId, notFoundParentCategoryId, default));

        categoryDataReaderMock.Verify();
    }

    [Fact]
    public async Task SetParentCategory_CircularParentCategory_ThrowsCategoryCircularRelationshipException()
    {
        var childCategory = new Category(Guid.NewGuid(), "child");
        var parentCategory = new Category(Guid.NewGuid(), "parent");
        var categoryDataReaderStub = CategoryDataReader
            .CategoryById(childCategory.Id, childCategory)
            .CategoryById(parentCategory.Id, parentCategory);
        await parentCategory.SetParentAsync(childCategory.Id, categoryDataReaderStub.Object);
        var categoryManager = new CategoryManager(null!, categoryDataReaderStub.Object);

        await Assert.ThrowsAsync<CategoryCircularRelationshipException>(()
            => categoryManager.SetParentCategoryAsync(childCategory.Id, parentCategory.Id, default));
    }

    [Fact]
    public async Task SetParentCategory_UpdateParentCategoryInfo()
    {
        var childCategory = new Category(Guid.NewGuid(), "child");
        var parentCategory = new Category(Guid.NewGuid(), "parent");
        var onParentDisplayOrder = 3;
        var returnCategory = new Category(childCategory.Id, childCategory.Name);
        var categoryRepositoryMock = Repository.Create<Category>()
            .WhenCall(repository => repository.UpdateAsync(
                It.Is<Category>(c => c.ParentId == parentCategory.Id), default),
                returnCategory
            );
        var categoryDataReaderMock = CategoryDataReader
            .CategoryById(childCategory.Id, childCategory)
            .CategoryById(parentCategory.Id, parentCategory);
        var categoryManager = new CategoryManager(categoryRepositoryMock.Object, categoryDataReaderMock.Object);

        var categoryDto = await categoryManager.SetParentCategoryAsync(childCategory.Id, parentCategory.Id, onParentDisplayOrder);

        Assert.Equal(categoryDto, returnCategory.ToDto());
        categoryRepositoryMock.Verify();
        categoryDataReaderMock.Verify();
    }

    #endregion

    #region DeleteCategoryAsync

    [Fact]
    public async Task DeleteCategoryAsync_CategoryIsNotFound_ThrowsArgumentException()
    {
        var notFoundCategoryId = Guid.NewGuid();
        var categoryDataReaderMock = CategoryDataReader.NotFound(notFoundCategoryId);
        var categoryManager = new CategoryManager(null!, categoryDataReaderMock.Object);

        await Assert.ThrowsAsync<CategoryIsNotFoundException>(()
            => categoryManager.DeleteCategoryAsync(notFoundCategoryId));

        categoryDataReaderMock.Verify();
    }

    [Fact]
    public async Task DeleteCategoryAsync_DeleteCategory()
    {
        var category = new Category(Guid.NewGuid(), "category");
        var categoryRepositoryMock = CategoryRepository.CanDeleteCategory(category);
        var categoryDataReaderMock = CategoryDataReader.CategoryById(category.Id, category);
        var categoryManager = new CategoryManager(categoryRepositoryMock.Object, categoryDataReaderMock.Object);

        await categoryManager.DeleteCategoryAsync(category.Id);

        categoryRepositoryMock.Verify();
        categoryDataReaderMock.Verify();
        Assert.Contains(category.DomainEvents, ev =>
            ev is CategoryDeleted deleted
            && deleted.CategoryId == category.Id
            && deleted.Name == category.Name);
    }

    [Fact]
    public async Task DeleteCategoryAsync_HasChildCategories_SetParentIdToNull()
    {
        var parent = new Category(Guid.NewGuid(), "parent");
        var child1 = new Category(Guid.NewGuid(), "child1");
        var child2 = new Category(Guid.NewGuid(), "child2");
        var categoryDataReaderMock = CategoryDataReader.CategoryById(parent.Id, parent)
            .AllCategories(child1, child2);
        await child1.SetParentAsync(parent.Id, categoryDataReaderMock.Object);
        await child2.SetParentAsync(parent.Id, categoryDataReaderMock.Object);
        var removedParentChild1 = new Category(child1.Id, child1.Name);
        var removedParentChild2 = new Category(child2.Id, child2.Name);
        var categoryRepositoryMock = CategoryRepository.CanDeleteCategory(parent)
            .WhenCall(repository =>
                repository.UpdateAsync(It.Is<Category>(c => c.Id == child1.Id && c.ParentId == null), default),
                removedParentChild1)
            .WhenCall(repository =>
                repository.UpdateAsync(It.Is<Category>(c => c.Id == child2.Id && c.ParentId == null), default),
                removedParentChild2);
        var categoryManager = new CategoryManager(categoryRepositoryMock.Object, categoryDataReaderMock.Object);

        await categoryManager.DeleteCategoryAsync(parent.Id);

        categoryRepositoryMock.Verify();
        categoryDataReaderMock.Verify();
    }

    #endregion
}
