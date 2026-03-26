using NamEcommerce.Data.Contracts;
using NamEcommerce.Domain.Entities.Catalog;
using NamEcommerce.Domain.Entities.Media;
using NamEcommerce.Domain.Services.Extensions;
using NamEcommerce.Domain.Shared.Dtos.Catalog;
using NamEcommerce.Domain.Shared.Dtos.Common;
using NamEcommerce.Domain.Shared.Exceptions.Catalog;
using NamEcommerce.Domain.Shared.Helpers;
using NamEcommerce.Domain.Shared.Services;
using NamEcommerce.Domain.Shared.Services.Catalog;

namespace NamEcommerce.Domain.Services.Catalog;

public sealed class ProductManager : IProductManager
{
    private readonly IRepository<Product> _productRepository;
    private readonly IEntityDataReader<Product> _productDataReader;
    private readonly IEntityDataReader<Category> _categoryDataReader;
    private readonly IEntityDataReader<Picture> _pictureDataReader;

    public ProductManager(IRepository<Product> productRepository,
        IEntityDataReader<Product> productEntityDataReader, IEntityDataReader<Category> categoryDataReader,
        IEntityDataReader<Picture> pictureDataReader)
    {
        _productRepository = productRepository;
        _productDataReader = productEntityDataReader;
        _categoryDataReader = categoryDataReader;
        _pictureDataReader = pictureDataReader;
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

        return new CreateProductResultDto
        {
            CreatedId = insertedProduct.Id
        };
    }

    public async Task DeleteProductAsync(Guid id)
    {
        var product = await _productDataReader.GetByIdAsync(id).ConfigureAwait(false);
        if (product is null)
            throw new ArgumentException("Product is not found", nameof(id));

        await _productRepository.DeleteAsync(product).ConfigureAwait(false);
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

    public Task<IPagedDataDto<ProductDto>> GetProductsAsync(string? keywords, int pageIndex, int pageSize)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(pageIndex, 0, nameof(pageIndex));
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(pageSize, 0, nameof(pageSize));

        var query = _productDataReader.DataSource;

        if (!string.IsNullOrEmpty(keywords))
        {
            var normizedKeywords = TextHelper.Normalize(keywords);
            query = query.Where(c =>
                c.NormalizedName.Contains(normizedKeywords)
            );
        }

        query = query.OrderBy(c => c.Name);

        var totalCount = query.Count();
        var pagedData = query
            .Skip(pageIndex * pageSize)
            .Take(pageSize)
            .ToList();

        var data = PagedDataDto.Create(pagedData.Select(product => product.ToDto()), pageIndex, pageSize, totalCount);
        return Task.FromResult(data);
    }

    public async Task<UpdateProductResultDto> UpdateProductAsync(UpdateProductDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        dto.Verify();

        var product = await _productDataReader.GetByIdAsync(dto.Id).ConfigureAwait(false);
        if (product is null)
            throw new ArgumentException("Product  is not found", nameof(dto));

        product.ShortDesc = dto.ShortDesc;
        product.UpdatedOnUtc = DateTime.UtcNow;

        await product.SetNameAsync(dto.Name, this);

        product.ClearProductCategories();
        foreach (var categoryInfo in dto.Categories)
            await product.AddToCategoryAsync(categoryInfo.CategoryId, categoryInfo.DisplayOrder, _categoryDataReader).ConfigureAwait(false);

        product.ClearProductPictures();
        foreach (var pictureId in dto.Pictures)
            await product.AddPictureAsync(pictureId, _pictureDataReader).ConfigureAwait(false);

        var result = await _productRepository.UpdateAsync(product).ConfigureAwait(false);

        return new UpdateProductResultDto(result.Id)
        {
            Name = result.Name,
            ShortDesc = result.ShortDesc,
        };
    }
}
