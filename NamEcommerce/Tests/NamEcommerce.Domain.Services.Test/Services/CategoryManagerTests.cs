﻿using NamEcommerce.Domain.Services.Extensions;
using NamEcommerce.Domain.Services.Test.Helpers;
using System.Linq.Expressions;

namespace NamEcommerce.Domain.Test.Services;

public sealed class CategoryManagerTests
{
    #region CreateCategoryAsync

    [Fact]
    public async Task CreateCategoryAsync_InputDtoIsNull_ThrowsArgumentNullException()
    {
        var categoryManager = new CategoryManager(null!, null!);

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            categoryManager.CreateCategoryAsync(null!)
        );
    }

    [Fact]
    public async Task CreateCategoryAsync_NameIsExists_ThrowsCategoryNameExistsException()
    {
        var testName = "existing-name";
        var categoryDataReaderMock = CategoryDataReader.HasOne(new Category(default, testName));
        var categoryManager = new CategoryManager(null!, categoryDataReaderMock.Object);

        await Assert.ThrowsAsync<CategoryNameExistsException>(() =>
            categoryManager.CreateCategoryAsync(new CreateCategoryDto(testName))
        );
        categoryDataReaderMock.Verify();
    }

    [Fact]
    public async Task CreateCategoryAsync_DtoIsValid_ReturnsCreatedCategoryDto()
    {
        var category = new Category(Guid.NewGuid(), "name") { DisplayOrder = 1 };
        var returnCategory = new Category(category.Id, category.Name)
        {
            DisplayOrder = category.DisplayOrder
        };
        var categoryRepositoryMock = CategoryRepository.CreateCategoryWillReturns(category, returnCategory);
        var categoryDataReaderMock = CategoryDataReader.Empty();
        var categoryManager = new CategoryManager(categoryRepositoryMock.Object, categoryDataReaderMock.Object);

        var categoryDto = await categoryManager.CreateCategoryAsync(
            new CreateCategoryDto(category.Name) { DisplayOrder = category.DisplayOrder });

        Assert.Equal(categoryDto, returnCategory.ToDto());
        categoryRepositoryMock.Verify();
        categoryDataReaderMock.Verify();
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
    public async Task UpdateCategoryAsync_CategoryIsNotFound_ThrowsArgumentException()
    {
        var notFoundCategoryId = Guid.NewGuid();
        var categoryRepositoryMock = CategoryRepository.NotFound(notFoundCategoryId);
        var categoryManager = new CategoryManager(categoryRepositoryMock.Object, null!);

        await Assert.ThrowsAsync<ArgumentException>(()
            => categoryManager.UpdateCategoryAsync(new UpdateCategoryDto(notFoundCategoryId, string.Empty)));
        categoryRepositoryMock.Verify();
    }

    [Fact]
    public async Task UpdateCategoryAsync_CategoryNameIsExists_ThrowsCategoryNameExistsException()
    {
        var oldCategory = new Category(Guid.NewGuid(), "parent-old");
        var updateCategory = oldCategory with
        {
            Name = "parent-new"
        };
        var sameNameCategoryId = Guid.NewGuid();
        var categoryRepositoryMock = CategoryRepository.CategoryById(oldCategory.Id, oldCategory);
        var categoryDataReaderMock = CategoryDataReader.HasOne(new Category(default, updateCategory.Name));
        var categoryManager = new CategoryManager(categoryRepositoryMock.Object, categoryDataReaderMock.Object);

        await Assert.ThrowsAsync<CategoryNameExistsException>(() 
            => categoryManager.UpdateCategoryAsync(new UpdateCategoryDto(updateCategory.Id, updateCategory.Name)));

        categoryRepositoryMock.Verify();
        categoryDataReaderMock.Verify();
    }

    [Fact]
    public async Task UpdateCategoryAsync_UpdateCategory()
    {
        var oldCategory = new Category(Guid.NewGuid(), "parent-old")
        {
            DisplayOrder = 1
        };
        var updateCategory = oldCategory with
        {
            Name = "parent-new",
            DisplayOrder = 2
        };
        Expression<Func<Category, bool>> isCategoryMatch =
            c => c.Id == updateCategory.Id
                && c.Name == updateCategory.Name
                && c.DisplayOrder == updateCategory.DisplayOrder;
        var categoryRepositoryMock = CategoryRepository.CategoryById(oldCategory.Id, oldCategory)
            .WhenCall(repository => repository.UpdateAsync(It.Is(isCategoryMatch), default), updateCategory);
        var categoryDataReaderMock = CategoryDataReader.Empty();
        var categoryManager = new CategoryManager(categoryRepositoryMock.Object, categoryDataReaderMock.Object);

        var resultCategory = await categoryManager.UpdateCategoryAsync(
            new UpdateCategoryDto(updateCategory.Id, updateCategory.Name)
            {
                DisplayOrder = updateCategory.DisplayOrder,
            });

        Assert.Equal(resultCategory, updateCategory.ToDto());
        categoryRepositoryMock.Verify();
        categoryDataReaderMock.Verify();
    }

    #endregion

    #region SetParentCategory

    [Fact]
    public async Task SetParentCategory_CategoryIsNotFound_ThrowsArgumentException()
    {
        var notFoundCategoryId = Guid.NewGuid();
        var categoryRepositoryMock = CategoryRepository.NotFound(notFoundCategoryId);
        var categoryManager = new CategoryManager(categoryRepositoryMock.Object, null!);

        await Assert.ThrowsAsync<ArgumentException>(()
            => categoryManager.SetParentCategory(notFoundCategoryId, default, default));

        categoryRepositoryMock.Verify(repository => repository.GetByIdAsync(notFoundCategoryId, default), Times.Once);
    }

    [Fact]
    public async Task SetParentCategory_ParentCategoryIsNotFound_ThrowsArgumentException()
    {
        var categoryId = Guid.NewGuid();
        var notFoundParentCategoryId = Guid.NewGuid();
        var categoryRepositoryMock = CategoryRepository
            .CategoryById(categoryId, new Category(categoryId, string.Empty))
            .NotFound(notFoundParentCategoryId);
        var categoryManager = new CategoryManager(categoryRepositoryMock.Object, null!);

        await Assert.ThrowsAsync<ArgumentException>(()
            => categoryManager.SetParentCategory(categoryId, notFoundParentCategoryId, default));

        categoryRepositoryMock.Verify(repository => repository.GetByIdAsync(categoryId, default), Times.Once);
        categoryRepositoryMock.Verify(repository => repository.GetByIdAsync(notFoundParentCategoryId, default), Times.Once);
    }

    [Fact]
    public async Task SetParentCategory_CircularParentCategory_ThrowsCategoryCircularRelationshipException()
    {
        var childCategory = new Category(Guid.NewGuid(), string.Empty);
        var parentCategory = new Category(Guid.NewGuid(), string.Empty)
        {
            ParentId = childCategory.Id
        };
        var categoryRepositoryStub = CategoryRepository
            .CategoryById(childCategory.Id, childCategory)
            .CategoryById(parentCategory.Id, parentCategory);
        var categoryManager = new CategoryManager(categoryRepositoryStub.Object, null!);

        await Assert.ThrowsAsync<CategoryCircularRelationshipException>(()
            => categoryManager.SetParentCategory(childCategory.Id, parentCategory.Id, default));
    }

    [Fact]
    public async Task SetParentCategory_UpdateParentCategoryInfo()
    {
        var childCategory = new Category(Guid.NewGuid(), string.Empty);
        var parentCategory = new Category(Guid.NewGuid(), string.Empty);
        var onParentDisplayOrder = 3;
        var returnCategory = childCategory with
        {
            ParentId = parentCategory.Id,
            OnParentDisplayOrder = onParentDisplayOrder
        };
        var categoryRepositoryMock = CategoryRepository
            .CategoryById(childCategory.Id, childCategory)
            .CategoryById(parentCategory.Id, parentCategory)
            .WhenCall(repository => repository.UpdateAsync(
                It.Is<Category>(c => c.ParentId == parentCategory.Id && c.OnParentDisplayOrder == onParentDisplayOrder), default),
                returnCategory
            );
        var categoryManager = new CategoryManager(categoryRepositoryMock.Object, null!);

        var categoryDto = await categoryManager.SetParentCategory(childCategory.Id, parentCategory.Id, onParentDisplayOrder);

        Assert.Equal(categoryDto, returnCategory.ToDto());
        categoryRepositoryMock.Verify();
    }

    #endregion

    #region DeleteCategoryAsync

    [Fact]
    public async Task DeleteCategoryAsync_CategoryIsNotFound_ThrowsArgumentException()
    {
        var notFoundCategoryId = Guid.NewGuid();
        var categoryRepositoryMock = CategoryRepository.NotFound(notFoundCategoryId);
        var categoryManager = new CategoryManager(categoryRepositoryMock.Object, null!);

        await Assert.ThrowsAsync<ArgumentException>(()
            => categoryManager.DeleteCategoryAsync(notFoundCategoryId));

        categoryRepositoryMock.Verify();
    }

    [Fact]
    public async Task DeleteCategoryAsync_DeleteCategory()
    {
        var category = new Category(Guid.NewGuid(), string.Empty);
        var categoryRepositoryMock = CategoryRepository.CategoryById(category.Id, category);
        var categoryManager = new CategoryManager(categoryRepositoryMock.Object, null!);

        await categoryManager.DeleteCategoryAsync(category.Id);

        categoryRepositoryMock.Verify();
    }

    [Fact]
    public async Task DeleteCategoryAsync_HasChildCategories_SetParentIdToNull()
    {
        var parent = new Category(Guid.NewGuid(), "parent");
        var child1 = new Category(Guid.NewGuid(), "child1") { ParentId = parent.Id };
        var child2 = new Category(Guid.NewGuid(), "child2") { ParentId = parent.Id };
        var categoryRepositoryMock = CategoryRepository
            .SetData(child1, child2)
            .CategoryById(parent.Id, parent)
            .WhenCall(repository => 
                repository.UpdateAsync(It.Is<Category>(c => c.Id == child1.Id && c.ParentId == null), default),
                child1 with
                {
                    ParentId = null
                })
            .WhenCall(repository => 
                repository.UpdateAsync(It.Is<Category>(c => c.Id == child2.Id && c.ParentId == null), default),
                child2 with
                {
                    ParentId = null
                });
        var categoryManager = new CategoryManager(categoryRepositoryMock.Object, null!);

        await categoryManager.DeleteCategoryAsync(parent.Id);

        categoryRepositoryMock.Verify();
    }

    #endregion
}
