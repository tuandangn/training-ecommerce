using NamEcommerce.Domain.Entities.Media;
using NamEcommerce.Domain.Services.Test.Helpers;
using NamEcommerce.Domain.Shared.Events;
using NamEcommerce.Domain.Shared.Exceptions.Media;
using System.Linq.Expressions;

namespace NamEcommerce.Domain.Services.Test.Services;

public sealed class ProductManagerTests
{
    #region CreateProductAsync

    [Fact]
    public async Task CreateProductAsync_DtoIsNull_ThrowArgumentNullException()
    {
        var productManager = new ProductManager(null!, null!, null!, null!, null!, null!);

        await Assert.ThrowsAsync<ArgumentNullException>(() => productManager.CreateProductAsync(null!));
    }

    [Fact]
    public async Task CreateProductAsync_DataIsInvalid_ThrowsProductDataIsInvalidException()
    {
        var invalidCreateProductDto = new CreateProductDto
        {
            Name = string.Empty,
            ShortDesc = "invalid-phone", //or empty
        };
        var productManager = new ProductManager(null!, null!, null!, null!, null!, null!);

        await Assert.ThrowsAsync<ProductDataIsInvalidException>(() => productManager.CreateProductAsync(invalidCreateProductDto));
    }

    [Fact]
    public async Task CreateProductAsync_NameIsExists_ThrowsProductNameExistsException()
    {
        var existingName = "existing-name";
        var dto = new CreateProductDto
        {
            Name = existingName,
            ShortDesc = "description"
        };
        var productDataReaderMock = ProductDataReader.HasOne(new Product(Guid.NewGuid(), existingName)
        {
            ShortDesc = "description"
        });
        var productManager = new ProductManager(null!, productDataReaderMock.Object, null!, null!, null!, null!);

        await Assert.ThrowsAsync<ProductNameExistsException>(() => productManager.CreateProductAsync(dto));
    }

    [Fact]
    public async Task CreateProductAsync_DataIsValid_ReturnsCreatedProduct()
    {
        var dto = new CreateProductDto
        {
            Name = "product-name",
            ShortDesc = "description"
        };
        var returnProduct = new Product(Guid.NewGuid(), dto.Name)
        {
            ShortDesc = dto.ShortDesc
        };
        var productRepositoryMock = ProductRepository.CreateProductWillReturns(returnProduct);
        var productDataReaderStub = ProductDataReader.Empty();
        var productManager = new ProductManager(productRepositoryMock.Object, productDataReaderStub.Object, null!, null!, Mock.Of<IEventPublisher>(), null!);

        var productDto = await productManager.CreateProductAsync(dto);

        Assert.Equal(returnProduct.Id, productDto.CreatedId);
        productRepositoryMock.Verify();
    }

    [Fact]
    public async Task CreateProductAsync_AddingCategoryNotFound_ThrowsCategoryIsNotFoundException()
    {
        var notFoundCategoryId = Guid.NewGuid();
        var dto = new CreateProductDto
        {
            Name = "product-name",
            ShortDesc = "description",
            Categories = [new ProductCategoryDto(notFoundCategoryId, 1)]
        };
        var categoryDataReaderMock = CategoryDataReader.NotFound(notFoundCategoryId); 
        var productDataReaderStub = ProductDataReader.Empty();
        var productManager = new ProductManager(null!, productDataReaderStub.Object, categoryDataReaderMock.Object, null!, null!, null!);

        await Assert.ThrowsAsync<CategoryIsNotFoundException>(() => productManager.CreateProductAsync(dto));

        categoryDataReaderMock.Verify();
    }

    [Fact]
    public async Task CreateProductAsync_AddingCategoryIsFound_AddProductToCategory()
    {
        var categoryId = Guid.NewGuid();
        var displayOrder = 1;
        var dto = new CreateProductDto
        {
            Name = "product-name",
            ShortDesc = "description",
            Categories = [new ProductCategoryDto(categoryId, displayOrder)]
        };
        var categoryDataReaderStub = CategoryDataReader.CategoryById(categoryId, new Category(categoryId, "category")); 
        var productDataReaderStub = ProductDataReader.Empty();
        var returnProduct = new Product(Guid.NewGuid(), dto.Name)
        {
            ShortDesc = dto.ShortDesc
        };
        await returnProduct.AddToCategoryAsync(categoryId, displayOrder, categoryDataReaderStub.Object);
        var productRepositoryMock = ProductRepository.CreateProductWillReturns(returnProduct);
        var productManager = new ProductManager(productRepositoryMock.Object, productDataReaderStub.Object, categoryDataReaderStub.Object, null!, Mock.Of<IEventPublisher>(), null!);

        var result = await productManager.CreateProductAsync(dto);

        Assert.Equal(returnProduct.Id, result.CreatedId);
        productRepositoryMock.Verify();
    }

    [Fact]
    public async Task CreateProductAsync_AddingUnitMeasurementNotFound_ThrowsArgumentException()
    {
        var notFoundUnitMeasurementId = Guid.NewGuid();
        var dto = new CreateProductDto
        {
            Name = "product-name",
            ShortDesc = "description",
            UnitMeasurementId = notFoundUnitMeasurementId
        };
        var unitMeasurementDataReaderMock = UnitMeasurementDataReader.NotFound(notFoundUnitMeasurementId);
        var productDataReaderStub = ProductDataReader.Empty();
        var productManager = new ProductManager(null!, productDataReaderStub.Object, null!, null!, null!, unitMeasurementDataReaderMock.Object);

        await Assert.ThrowsAsync<ArgumentException>(() => productManager.CreateProductAsync(dto));

        unitMeasurementDataReaderMock.Verify();
    }

    [Fact]
    public async Task CreateProductAsync_AddingUnitMeasurementIsFound_SetUnitMeasurementForProduct()
    {
        var unitMeasurementId = Guid.NewGuid();
        var displayOrder = 1;
        var dto = new CreateProductDto
        {
            Name = "product-name",
            ShortDesc = "description",
            UnitMeasurementId = unitMeasurementId
        };
        var unitMeasurementDataReaderStub = UnitMeasurementDataReader.UnitMeasurementById(unitMeasurementId, new UnitMeasurement(unitMeasurementId, "unit-measurement"));
        var productDataReaderStub = ProductDataReader.Empty();
        var returnProduct = new Product(Guid.NewGuid(), dto.Name)
        {
            ShortDesc = dto.ShortDesc
        };
        await returnProduct.SetUnitMeasurementAsync(unitMeasurementId, unitMeasurementDataReaderStub.Object);
        var productRepositoryMock = ProductRepository.CreateProductWillReturns(returnProduct);
        var productManager = new ProductManager(productRepositoryMock.Object, productDataReaderStub.Object, null!, null!, Mock.Of<IEventPublisher>(), unitMeasurementDataReaderStub.Object);

        var result = await productManager.CreateProductAsync(dto);

        Assert.Equal(returnProduct.Id, result.CreatedId);
        productRepositoryMock.Verify();
    }

    [Fact]
    public async Task CreateProductAsync_AddingPictureNotFound_ThrowsPictureIsNotFoundException()
    {
        var notFoundPictureId = Guid.NewGuid();
        var dto = new CreateProductDto
        {
            Name = "product-name",
            ShortDesc = "description",
            Pictures = [notFoundPictureId]
        };
        var pictureDataReaderMock = PictureDataReader.NotFound(notFoundPictureId);
        var productDataReaderStub = ProductDataReader.Empty();
        var productManager = new ProductManager(null!, productDataReaderStub.Object, null!, pictureDataReaderMock.Object, null!, null!);

        await Assert.ThrowsAsync<PictureIsNotFoundException>(() => productManager.CreateProductAsync(dto));

        pictureDataReaderMock.Verify();
    }

    [Fact]
    public async Task CreateProductAsync_AddingPictureIsFound_AddPictureToProduct()
    {
        var picture = new Picture([], ".jpg");
        var pictureId = picture.Id;
        var dto = new CreateProductDto
        {
            Name = "product-name",
            ShortDesc = "description",
            Pictures = [pictureId]
        };
        var pictureDataReaderStub = PictureDataReader.PictureById(pictureId, picture);
        var returnProduct = new Product(Guid.NewGuid(), dto.Name)
        {
            ShortDesc = dto.ShortDesc
        };
        await returnProduct.AddPictureAsync(pictureId, pictureDataReaderStub.Object);
        var productDataReaderStub = ProductDataReader.Empty();
        var productRepositoryMock = ProductRepository.CreateProductWillReturns(returnProduct);
        var productManager = new ProductManager(productRepositoryMock.Object, productDataReaderStub.Object, null!, pictureDataReaderStub.Object, Mock.Of<IEventPublisher>(), null!);

        var result = await productManager.CreateProductAsync(dto);

        Assert.Equal(returnProduct.Id, result.CreatedId);
        productRepositoryMock.Verify();
    }

    #endregion

    #region DoesNameExistAsync

    [Fact]
    public async Task DoesNameExistAsync_NameIsNull_ThrowsArgumentNullException()
    {
        var productManager = new ProductManager(null!, null!, null!, null!, null!, null!);

        await Assert.ThrowsAnyAsync<ArgumentException>(() =>
            productManager.DoesNameExistAsync(null!)
        );
    }

    [Fact]
    public async Task DoesNameExistAsync_NameIsMatchAndCompareIdEquals_ReturnsFalse()
    {
        var hasNameProductId = Guid.NewGuid();
        var testName = "test-name-existing";
        var productDataReaderMock = ProductDataReader.HasOne(new Product(hasNameProductId, testName) { ShortDesc = "short-description" });
        var productManager = new ProductManager(null!, productDataReaderMock.Object, null!, null!, null!, null!);

        var nameExists = await productManager.DoesNameExistAsync(testName, comparesWithCurrentId: hasNameProductId);

        Assert.False(nameExists);
        productDataReaderMock.Verify();
    }

    [Fact]
    public async Task DoesNameExistAsync_NameIsMatchAndCompareIdIsNotProvided_ReturnsTrue()
    {
        var testName = "test-name-existing";
        var productDataReaderMock = ProductDataReader.HasOne(new Product(default, testName) { ShortDesc = "short-description" });
        var productManager = new ProductManager(null!, productDataReaderMock.Object, null!, null!, null!, null!);

        var nameExists = await productManager.DoesNameExistAsync(testName, comparesWithCurrentId: null);

        Assert.True(nameExists);
        productDataReaderMock.Verify();
    }

    #endregion

    #region UpdateProductAsync

    [Fact]
    public async Task UpdateProductAsync_DtoIsNull_ThrowsArgumentNullException()
    {
        var productManager = new ProductManager(null!, null!, null!, null!, null!, null!);

        await Assert.ThrowsAsync<ArgumentNullException>(() => productManager.UpdateProductAsync(null!));
    }

    [Fact]
    public async Task UpdateProductAsync_DataIsInvalid_ThrowsProductDataIsInvalidException()
    {
        var productManager = new ProductManager(null!, null!, null!, null!, null!, null!);

        await Assert.ThrowsAsync<ProductDataIsInvalidException>(() =>
            productManager.UpdateProductAsync(new UpdateProductDto(Guid.NewGuid())
            {
                Name = string.Empty,
                ShortDesc = string.Empty
            })
        );
    }

    [Fact]
    public async Task UpdateProductAsync_ProductIsNotFound_ThrowsArgumentException()
    {
        var notFoundProductId = Guid.NewGuid();
        var productDataReaderMock = ProductDataReader.NotFound(notFoundProductId);
        var productManager = new ProductManager(null!, productDataReaderMock.Object, null!, null!, null!, null!);

        await Assert.ThrowsAsync<ProductIsNotFoundException>(()
            => productManager.UpdateProductAsync(new UpdateProductDto(notFoundProductId)
            {
                Name = "product",
                ShortDesc = "0123456789"
            }));
        productDataReaderMock.Verify();
    }

    [Fact]
    public async Task UpdateProductAsync_ProductNameIsExists_ThrowsProductNameExistsException()
    {
        var oldProduct = new Product(Guid.NewGuid(), "old-product-name")
        {
            ShortDesc = "0123456789"
        };
        var updateProduct = new Product(oldProduct.Id, "new-product-name")
        {
            ShortDesc = "0123456789"
        };
        var sameNameCategoryId = Guid.NewGuid();
        var productDataReaderMock = ProductDataReader
            .HasOne(new Product(default, updateProduct.Name)
            {
                ShortDesc = "0123456789"
            })
            .ProductById(oldProduct.Id, oldProduct);
        var productManager = new ProductManager(null!, productDataReaderMock.Object, null!, null!, null!, null!);

        await Assert.ThrowsAsync<ProductNameExistsException>(()
            => productManager.UpdateProductAsync(new UpdateProductDto(updateProduct.Id)
            {
                Name = updateProduct.Name,
                ShortDesc = updateProduct.ShortDesc
            }));

        productDataReaderMock.Verify();
    }

    [Fact]
    public async Task UpdateProductAsync_UpdateProduct()
    {
        var oldProduct = new Product(Guid.NewGuid(), "old-product-name")
        {
            ShortDesc = "short-description"
        };
        var updateProduct = new Product(oldProduct.Id, "new-product-name")
        {
            ShortDesc = "short-description"
        };
        Expression<Func<Product, bool>> isProductMatch =
            c => c.Id == updateProduct.Id
                && c.Name == updateProduct.Name
                && c.ShortDesc == updateProduct.ShortDesc;
        var productRepositoryMock = Repository.Create<Product>()
            .WhenCall(repository => repository.UpdateAsync(It.Is(isProductMatch), default), updateProduct);
        var productDataReaderStub = ProductDataReader.ProductById(oldProduct.Id, oldProduct);
        var productManager = new ProductManager(productRepositoryMock.Object, productDataReaderStub.Object, null!, null!, Mock.Of<IEventPublisher>(), null!);

        var resultProduct = await productManager.UpdateProductAsync(
            new UpdateProductDto(updateProduct.Id)
            {
                Name = updateProduct.Name,
                ShortDesc = updateProduct.ShortDesc
            });

        Assert.Equal(resultProduct, resultProduct with
        {
            Id = updateProduct.Id,
            Name = updateProduct.Name,
            ShortDesc = updateProduct.ShortDesc
        });
        productRepositoryMock.Verify();
    }

    [Fact]
    public async Task UpdateProductAsync_UpdatingCategoryNotFound_ThrowsCategoryIsNotFoundException()
    {
        var notFoundCategoryId = Guid.NewGuid();
        var dto = new UpdateProductDto(Guid.NewGuid())
        {
            Name = "product-name",
            ShortDesc = "description",
            Categories = [new ProductCategoryDto(notFoundCategoryId, 1)]
        };
        var categoryDataReaderMock = CategoryDataReader.NotFound(notFoundCategoryId);
        var productDataReaderStub = ProductDataReader.ProductById(dto.Id, new Product(dto.Id, "old-product")
        {
            ShortDesc = "old-description"
        });
        var productManager = new ProductManager(null!, productDataReaderStub.Object, categoryDataReaderMock.Object, null!, null!, null!);

        await Assert.ThrowsAsync<CategoryIsNotFoundException>(() => productManager.UpdateProductAsync(dto));

        categoryDataReaderMock.Verify();
    }

    [Fact]
    public async Task UpdateProductAsync_UpdatingCategoryIsFound_AddProductToCategory()
    {
        var categoryId = Guid.NewGuid();
        var displayOrder = 1;
        var dto = new UpdateProductDto(Guid.NewGuid())
        {
            Name = "product-name",
            ShortDesc = "description",
            Categories = [new ProductCategoryDto(categoryId, displayOrder)]
        };
        var categoryDataReaderStub = CategoryDataReader.CategoryById(categoryId, new Category(categoryId, "category"));
        var productDataReaderStub = ProductDataReader.ProductById(dto.Id, new Product(dto.Id, "old-product") { ShortDesc = "old-description" });
        var returnProduct = new Product(Guid.NewGuid(), dto.Name) { ShortDesc = dto.ShortDesc };
        await returnProduct.AddToCategoryAsync(categoryId, displayOrder, categoryDataReaderStub.Object);
        var productRepositoryMock = ProductRepository.UpdateProductWillReturns(returnProduct);
        var productManager = new ProductManager(productRepositoryMock.Object, productDataReaderStub.Object, categoryDataReaderStub.Object, null!, Mock.Of<IEventPublisher>(), null!);

        var result = await productManager.UpdateProductAsync(dto);

        Assert.Equal(returnProduct.Id, result.Id);
        Assert.Single(result.Categories);
        Assert.Equal(categoryId, result.Categories.First().CategoryId);
        productRepositoryMock.Verify();
    }

    [Fact]
    public async Task UpdateProductAsync_UpdatingUnitMeasurementNotFound_ThrowsArgumentException()
    {
        var notFoundUnitMeasurementId = Guid.NewGuid();
        var dto = new UpdateProductDto(Guid.NewGuid())
        {
            Name = "product-name",
            ShortDesc = "description",
            UnitMeasurementId = notFoundUnitMeasurementId
        };
        var unitMeasurementDataReaderMock = UnitMeasurementDataReader.NotFound(notFoundUnitMeasurementId);
        var productDataReaderStub = ProductDataReader.ProductById(dto.Id, new Product(dto.Id, "old-product")
        {
            ShortDesc = "old-description"
        });
        var productManager = new ProductManager(null!, productDataReaderStub.Object, null!, null!, null!, unitMeasurementDataReaderMock.Object);

        await Assert.ThrowsAsync<ArgumentException>(() => productManager.UpdateProductAsync(dto));

        unitMeasurementDataReaderMock.Verify();
    }

    [Fact]
    public async Task UpdateProductAsync_UpdatingUnitMeasurementIsFound_AddProductToCategory()
    {
        var unitMeasurementId = Guid.NewGuid();
        var dto = new UpdateProductDto(Guid.NewGuid())
        {
            Name = "product-name",
            ShortDesc = "description",
            UnitMeasurementId = unitMeasurementId
        };
        var unitMeasurementDataReaderStub = UnitMeasurementDataReader.UnitMeasurementById(unitMeasurementId, new UnitMeasurement(unitMeasurementId, "unit-measurement"));
        var productDataReaderStub = ProductDataReader.ProductById(dto.Id, new Product(dto.Id, "old-product") { ShortDesc = "old-description" });
        var returnProduct = new Product(Guid.NewGuid(), dto.Name) { ShortDesc = dto.ShortDesc };
        await returnProduct.SetUnitMeasurementAsync(unitMeasurementId, unitMeasurementDataReaderStub.Object);
        var productRepositoryMock = ProductRepository.UpdateProductWillReturns(returnProduct);
        var productManager = new ProductManager(productRepositoryMock.Object, productDataReaderStub.Object, null!, null!, Mock.Of<IEventPublisher>(), unitMeasurementDataReaderStub.Object);

        var result = await productManager.UpdateProductAsync(dto);

        Assert.Equal(returnProduct.Id, result.Id);
        Assert.Equal(unitMeasurementId, result.UnitMeasurementId);
        productRepositoryMock.Verify();
    }

    [Fact]
    public async Task UpdateProductAsync_AddingPictureNotFound_ThrowsPictureIsNotFoundException()
    {
        var notFoundPictureId = Guid.NewGuid();
        var dto = new UpdateProductDto(Guid.NewGuid())
        {
            Name = "product-name",
            ShortDesc = "description",
            Pictures = [notFoundPictureId]
        };
        var pictureDataReaderMock = PictureDataReader.NotFound(notFoundPictureId);
        var productDataReaderStub = ProductDataReader.ProductById(dto.Id, new Product(dto.Id, "old-product"));
        var productManager = new ProductManager(null!, productDataReaderStub.Object, null!, pictureDataReaderMock.Object, null!, null!);

        await Assert.ThrowsAsync<PictureIsNotFoundException>(() => productManager.UpdateProductAsync(dto));

        pictureDataReaderMock.Verify();
    }

    [Fact]
    public async Task UpdateProductAsync_AddingPictureIsFound_AddPictureToProduct()
    {
        var picture = new Picture([], ".jpg");
        var pictureId = picture.Id;
        var dto = new UpdateProductDto(Guid.NewGuid())
        {
            Name = "product-name",
            ShortDesc = "description",
            Pictures = [pictureId]
        };
        var pictureDataReaderStub = PictureDataReader.PictureById(pictureId, picture);
        var returnProduct = new Product(Guid.NewGuid(), dto.Name)
        {
            ShortDesc = dto.ShortDesc
        };
        await returnProduct.AddPictureAsync(pictureId, pictureDataReaderStub.Object);
        var productDataReaderStub = ProductDataReader.ProductById(dto.Id, new Product(dto.Id, "old-product-name"));
        var productRepositoryMock = ProductRepository.UpdateProductWillReturns(returnProduct);
        var productManager = new ProductManager(productRepositoryMock.Object, productDataReaderStub.Object, null!, pictureDataReaderStub.Object, Mock.Of<IEventPublisher>(), null!);

        var result = await productManager.UpdateProductAsync(dto);

        Assert.Equal(returnProduct.Id, result.Id);
        Assert.Single(result.Pictures);
        Assert.Equal(pictureId, result.Pictures.First());
        productRepositoryMock.Verify();
    }

    #endregion

    #region DeleteProductAsync

    [Fact]
    public async Task DeleteProductAsync_ProductIsNotFound_ThrowsArgumentException()
    {
        var notFoundProductId = Guid.NewGuid();
        var productDataReaderMock = ProductDataReader.NotFound(notFoundProductId);
        var productManager = new ProductManager(null!, productDataReaderMock.Object, null!, null!, null!, null!);

        await Assert.ThrowsAsync<ProductIsNotFoundException>(()
            => productManager.DeleteProductAsync(notFoundProductId));

        productDataReaderMock.Verify();
    }

    [Fact]
    public async Task DeleteProductAsync_DeleteProduct()
    {
        var product = new Product(Guid.NewGuid(), "product")
        {
            ShortDesc = "short-description"
        };
        var productDataRepositoryMock = ProductRepository.CanDeleteProduct(product);
        var productDataReaderMock = ProductDataReader.ProductById(product.Id, product);
        var productManager = new ProductManager(productDataRepositoryMock.Object, productDataReaderMock.Object, null!, null!, Mock.Of<IEventPublisher>(), null!);

        await productManager.DeleteProductAsync(product.Id);

        productDataReaderMock.Verify();
    }

    #endregion

    #region GetProductsAsync

    [Fact]
    public async Task GetProductsAsync_PageIndexLessThanZero_ThrowsArgumentOutOfRangeException()
    {
        var invalidPageIndex = -1;
        var productManager = new ProductManager(null!, null!, null!, null!, null!, null!);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            productManager.GetProductsAsync(invalidPageIndex, int.MaxValue, "keywords"));
    }

    [Fact]
    public async Task GetProductsAsync_PageSizeLessThanOrEqualZero_ThrowsArgumentOutOfRangeException()
    {
        var invalidPageSize = 0;
        var productManager = new ProductManager(null!, null!, null!, null!, null!, null!);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            productManager.GetProductsAsync(0, invalidPageSize, "keywords"));
    }

    [Fact]
    public async Task GetProductsAsync_KeywordsIsEmpty_ReturnPagedOrderedData()
    {
        var pageIndex = 0;
        var pageSize = 2;
        var product1 = new Product(Guid.NewGuid(), "product-1")
        {
            ShortDesc = "short-description"
        };
        var product2 = new Product(Guid.NewGuid(), "product-2")
        {
            ShortDesc = "short-description"
        };
        var product3 = new Product(Guid.NewGuid(), "product-3")
        {
            ShortDesc = "short-description"
        };
        var productDataReaderMock = ProductDataReader.WithData(product1, product2, product3);
        var productManager = new ProductManager(null!, productDataReaderMock.Object, null!, null!, null!, null!);

        var pagedOrderedResult = await productManager.GetProductsAsync(pageIndex, pageSize, "");

        Assert.Equal(3, pagedOrderedResult.PagerInfo.TotalCount);
        Assert.Equal(product1.Id, pagedOrderedResult.ElementAt(0).Id);
        Assert.Equal(product2.Id, pagedOrderedResult.ElementAt(1).Id);
        productDataReaderMock.Verify();
    }

    [Fact]
    public async Task GetProductsAsync_KeywordsIsIncluded_ReturnFilteredData()
    {
        var keywords = "keywords";
        var pageIndex = 1; //second page
        var pageSize = 1;
        var product1 = new Product(Guid.NewGuid(), "keywords-1")
        {
            ShortDesc = "short-description"
        };
        var product2 = new Product(Guid.NewGuid(), "keywords-2")
        {
            ShortDesc = "short-description"
        };
        var product3 = new Product(Guid.NewGuid(), "product")
        {
            ShortDesc = "short-description"
        };
        var productDataReaderMock = ProductDataReader.WithData(product1, product2, product3);
        var productManager = new ProductManager(null!, productDataReaderMock.Object, null!, null!, null!, null!);

        var filteredResult = await productManager.GetProductsAsync(pageIndex, pageSize, keywords);

        Assert.Equal(2, filteredResult.PagerInfo.TotalCount);
        Assert.Equal(product2.Id, filteredResult.ElementAt(0).Id);
        productDataReaderMock.Verify();
    }

    #endregion

    #region RemoveProductFromCategoryAsync

    [Fact]
    public async Task RemoveProductFromCategoryAsync_ProductIsNotFound_ThrowsArgumentException()
    {
        var notFoundProductId = Guid.NewGuid();
        var productDataReaderMock = ProductDataReader.NotFound(notFoundProductId);
        var productManager = new ProductManager(null!, productDataReaderMock.Object, null!, null!, null!, null!);

        await Assert.ThrowsAsync<ProductIsNotFoundException>(()
            => productManager.RemoveProductFromCategoryAsync(notFoundProductId, default));

        productDataReaderMock.Verify();
    }

    [Fact]
    public async Task RemoveProductFromCategoryAsync_ProductIsFound_UpdateProduct()
    {
        var categoryId = Guid.NewGuid();
        var product = new Product(Guid.NewGuid(), "product");
        var categoryDataReaderStub = CategoryDataReader.CategoryById(categoryId, new Category(categoryId, "category"));
        await product.AddToCategoryAsync(categoryId, 1, categoryDataReaderStub.Object);
        var productDataReaderMock = ProductDataReader.ProductById(product.Id, product);

        var productDataRepositoryMock = new Mock<IRepository<Product>>();
        productDataRepositoryMock.Setup(repository => repository.UpdateAsync(It.Is<Product>(p => 
            p.Id == product.Id && p.Name == p.Name
            && p.ProductCategories.Count() == 0
        ))).ReturnsAsync(product);
        var productManager = new ProductManager(productDataRepositoryMock.Object, productDataReaderMock.Object, null!, null!, Mock.Of<IEventPublisher>(), null!);

        await productManager.RemoveProductFromCategoryAsync(product.Id, categoryId);

        productDataReaderMock.Verify();
    }

    #endregion
}
