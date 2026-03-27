using NamEcommerce.Data.Contracts;
using NamEcommerce.Domain.Entities.Catalog;
using NamEcommerce.Domain.Entities.Media;
using NamEcommerce.Domain.Services.Extensions;
using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Domain.Shared.Dtos.Catalog;
using NamEcommerce.Domain.Shared.Dtos.Common;
using NamEcommerce.Domain.Shared.Events;
using NamEcommerce.Domain.Shared.Exceptions.Catalog;
using NamEcommerce.Domain.Shared.Helpers;
using NamEcommerce.Domain.Shared.Services.Catalog;

namespace NamEcommerce.Domain.Services.Catalog;

public sealed class ProductManager : IProductManager
{
    private readonly IRepository<Product> _productRepository;
    private readonly IEntityDataReader<Product> _productDataReader;
    private readonly IEntityDataReader<Category> _categoryDataReader;
    private readonly IEntityDataReader<Picture> _pictureDataReader;
    private readonly IEventPublisher _eventPublisher;

    public ProductManager(IRepository<Product> productRepository,
        IEntityDataReader<Product> productEntityDataReader, IEntityDataReader<Category> categoryDataReader,
        IEntityDataReader<Picture> pictureDataReader, IEventPublisher eventPublisher)
    {
        _productRepository = productRepository;
        _productDataReader = productEntityDataReader;
        _categoryDataReader = categoryDataReader;
        _pictureDataReader = pictureDataReader;
        _eventPublisher = eventPublisher;
    }

    public async Task<CreateProductResultDto> CreateProductAsync(CreateProductDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        dto.Verify();

        if (await DoesNameExistAsync(dto.Name).ConfigureAwait(false))
            throw new ProductNameExistsException(dto.Name);

        var product = new Product(Guid.NewGuid(), dto.Name)
        {
            ShortDesc = dto.ShortDesc
        };
        foreach (var categoryInfo in dto.Categories)
            await product.AddToCategoryAsync(categoryInfo.CategoryId, categoryInfo.DisplayOrder, _categoryDataReader).ConfigureAwait(false);
        foreach (var pictureId in dto.Pictures)
            await product.AddPictureAsync(pictureId, _pictureDataReader).ConfigureAwait(false);

        var insertedProduct = await _productRepository.InsertAsync(product).ConfigureAwait(false);

        await _eventPublisher.EntityCreated(insertedProduct).ConfigureAwait(false);

        return new CreateProductResultDto
        {
            CreatedId = insertedProduct.Id
        };
    }

    public async Task DeleteProductAsync(Guid id)
    {
        var product = await _productDataReader.GetByIdAsync(id).ConfigureAwait(false);
        if (product is null)
            throw new ProductIsNotFoundException(id);

        await _productRepository.DeleteAsync(product).ConfigureAwait(false);

        await _eventPublisher.EntityDeleted(product).ConfigureAwait(false);
    }

    public Task<bool> DoesNameExistAsync(string name, Guid? comparesWithCurrentId = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        var query = from product in _productDataReader.DataSource
                    where product.Name == name && (comparesWithCurrentId == null || product.Id != comparesWithCurrentId)
                    select product;

        var sameNameExists = query.FirstOrDefault() != null;
        return Task.FromResult(sameNameExists);
    }

    public Task<IPagedDataDto<ProductDto>> GetProductsAsync(int pageIndex, int pageSize, string? keywords = null, Guid? categoryId = null)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(pageIndex, 0, nameof(pageIndex));
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(pageSize, 0, nameof(pageSize));

        var query = _productDataReader.DataSource;

        if (!string.IsNullOrEmpty(keywords))
        {
            var normizedKeywords = TextHelper.Normalize(keywords);
            query = query.Where(c => c.NormalizedName.Contains(normizedKeywords));
        }

        if (categoryId.HasValue)
            query = query.Where(c => c.ProductCategories.Any(pc => pc.CategoryId == categoryId));

        query = query.OrderBy(c => c.Name);

        var totalCount = query.Count();
        var pagedData = query
            .Skip(pageIndex * pageSize)
            .Take(pageSize)
            .ToList();

        var data = PagedDataDto.Create(pagedData.Select(product => product.ToDto()), pageIndex, pageSize, totalCount);
        return Task.FromResult(data);
    }

    public async Task RemoveProductFromCategoryAsync(Guid productId, Guid categoryId)
    {
        var product = await _productDataReader.GetByIdAsync(productId);
        if (product is null)
            throw new ProductIsNotFoundException(productId);

        product.RemoveFromCategory(categoryId);
        await _productRepository.UpdateAsync(product).ConfigureAwait(false);
    }

    public async Task<UpdateProductResultDto> UpdateProductAsync(UpdateProductDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        dto.Verify();

        var product = await _productDataReader.GetByIdAsync(dto.Id).ConfigureAwait(false);
        if (product is null)
            throw new ProductIsNotFoundException(dto.Id);

        product.ShortDesc = dto.ShortDesc;
        product.UpdatedOnUtc = DateTime.UtcNow;

        await product.SetNameAsync(dto.Name, this).ConfigureAwait(false);

        product.ClearProductCategories();
        foreach (var categoryInfo in dto.Categories)
            await product.AddToCategoryAsync(categoryInfo.CategoryId, categoryInfo.DisplayOrder, _categoryDataReader).ConfigureAwait(false);

        var deletedPictureIds = product.ProductPictures.Select(p => p.PictureId).ToList();
        product.ClearProductPictures();
        foreach (var pictureId in dto.Pictures)
            await product.AddPictureAsync(pictureId, _pictureDataReader).ConfigureAwait(false);

        var result = await _productRepository.UpdateAsync(product).ConfigureAwait(false);

        await _eventPublisher.EntityUpdated(result, deletedPictureIds).ConfigureAwait(false);

        return new UpdateProductResultDto(result.Id)
        {
            Name = result.Name,
            ShortDesc = result.ShortDesc,
            Categories = result.ProductCategories.Select(pc => new ProductCategoryDto(pc.CategoryId, pc.DisplayOrder)),
            Pictures = result.ProductPictures.Select(pp => pp.PictureId)
        };
    }
}
