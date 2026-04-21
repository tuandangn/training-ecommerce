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
    private readonly IEntityDataReader<UnitMeasurement> _unitMeasurementDataReader;
    private readonly IRepository<ProductPriceHistory> _priceHistoryRepository;
    private readonly IEntityDataReader<ProductPriceHistory> _priceHistoryDataReader;
    private readonly IEntityDataReader<Vendor> _vendorDataReader;
    private readonly IEventPublisher _eventPublisher;

    public ProductManager(IRepository<Product> productRepository,
        IEntityDataReader<Product> productEntityDataReader, IEntityDataReader<Category> categoryDataReader,
        IEntityDataReader<Picture> pictureDataReader, IEventPublisher eventPublisher,
        IEntityDataReader<UnitMeasurement> unitMeasurementDataReader,
        IRepository<ProductPriceHistory> priceHistoryRepository,
        IEntityDataReader<ProductPriceHistory> priceHistoryDataReader,
        IEntityDataReader<Vendor> vendorDataReader)
    {
        _productRepository = productRepository;
        _productDataReader = productEntityDataReader;
        _categoryDataReader = categoryDataReader;
        _pictureDataReader = pictureDataReader;
        _unitMeasurementDataReader = unitMeasurementDataReader;
        _priceHistoryRepository = priceHistoryRepository;
        _priceHistoryDataReader = priceHistoryDataReader;
        _vendorDataReader = vendorDataReader;
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
        product.UpdatePrice(dto.UnitPrice, dto.CostPrice);

        await product.SetUnitMeasurementAsync(dto.UnitMeasurementId, _unitMeasurementDataReader).ConfigureAwait(false);

        foreach (var categoryInfo in dto.Categories)
            await product.AddToCategoryAsync(categoryInfo.CategoryId, categoryInfo.DisplayOrder, _categoryDataReader).ConfigureAwait(false);
        foreach (var pictureId in dto.Pictures)
            await product.AddPictureAsync(pictureId, _pictureDataReader).ConfigureAwait(false);
        foreach (var vendorInfo in dto.Vendors)
            await product.AddVendorAsync(vendorInfo.VendorId, vendorInfo.DisplayOrder, _vendorDataReader).ConfigureAwait(false);

        var insertedProduct = await _productRepository.InsertAsync(product).ConfigureAwait(false);

        await _priceHistoryRepository.InsertAsync(new ProductPriceHistory(
            insertedProduct.Id, 0, insertedProduct.UnitPrice,
            0, insertedProduct.CostPrice,
            "Giá bán ban đầu")
        ).ConfigureAwait(false);

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

        return Task.Run(() => queryData());

        //local method
        async Task<IPagedDataDto<ProductDto>> queryData()
        {
            var query = _productDataReader.DataSource;

            if (!string.IsNullOrEmpty(keywords))
            {
                var normalizedKeywords = TextHelper.Normalize(keywords);
                var uppercaseKeywords = keywords.Trim().ToUpper();

                var vendorIds = _vendorDataReader.DataSource
                    .Where(c => c.Name.ToUpper().Contains(uppercaseKeywords) || c.Name.ToUpper().Contains(normalizedKeywords) || c.NormalizedName.Contains(normalizedKeywords))
                    .Select(v => v.Id)
                    .ToList()
                    .OfType<Guid?>()
                    .ToList();

                var categoryIds = _categoryDataReader.DataSource
                    .Where(c => c.Name.ToUpper().Contains(uppercaseKeywords) || c.Name.ToUpper().Contains(normalizedKeywords) || c.NormalizedName.Contains(normalizedKeywords))
                    .Select(v => v.Id)
                    .ToList()
                    .OfType<Guid?>()
                    .ToList();

                query = query.Where(product => product.Name.ToUpper().Contains(keywords)
                    || product.Name.ToUpper().Contains(normalizedKeywords)
                    || product.NormalizedName.Contains(normalizedKeywords)
                    || product.ProductVendors.Any(pv => vendorIds.Contains(pv.VendorId))
                    || product.ProductCategories.Any(pc => categoryIds.Contains(pc.CategoryId)));
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
            return data;
        }
    }

    public Task<IEnumerable<ProductDto>> GetProductsByVendorIdAsync(Guid vendorId)
    {
        return Task.Run(async () =>
        {
            var query = _productDataReader.DataSource
                .Where(p => p.ProductVendors.Any(pv => pv.VendorId == vendorId))
                .OrderBy(p => p.Name);

            var list = query.ToList().Select(p => p.ToDto()).ToList();
            return (IEnumerable<ProductDto>)list;
        });
    }

    public async Task AddProductVendorAsync(Guid productId, Guid vendorId, int displayOrder)
    {
        var product = await _productDataReader.GetByIdAsync(productId).ConfigureAwait(false);
        if (product is null)
            throw new ProductIsNotFoundException(productId);

        await product.AddVendorAsync(vendorId, displayOrder, _vendorDataReader).ConfigureAwait(false);
        await _productRepository.UpdateAsync(product).ConfigureAwait(false);
    }

    public async Task RemoveProductVendorAsync(Guid productId, Guid vendorId)
    {
        var product = await _productDataReader.GetByIdAsync(productId).ConfigureAwait(false);
        if (product is null)
            throw new ProductIsNotFoundException(productId);

        product.RemoveVendor(vendorId);
        await _productRepository.UpdateAsync(product).ConfigureAwait(false);
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

        var oldUnitPrice = product.UnitPrice;
        var oldCostPrice = product.CostPrice;

        product.ShortDesc = dto.ShortDesc;
        product.UpdatedOnUtc = DateTime.UtcNow;

        await product.SetNameAsync(dto.Name, this).ConfigureAwait(false);
        await product.SetUnitMeasurementAsync(dto.UnitMeasurementId, _unitMeasurementDataReader).ConfigureAwait(false);
        product.UpdatePrice(dto.UnitPrice, dto.CostPrice);

        product.ClearCategories();
        foreach (var categoryInfo in dto.Categories)
            await product.AddToCategoryAsync(categoryInfo.CategoryId, categoryInfo.DisplayOrder, _categoryDataReader).ConfigureAwait(false);

        var deletedPictureIds = product.ProductPictures.Select(p => p.PictureId).ToList();
        product.ClearPictures();
        foreach (var pictureId in dto.Pictures)
            await product.AddPictureAsync(pictureId, _pictureDataReader).ConfigureAwait(false);

        product.ClearVendors();
        foreach (var vendorInfo in dto.Vendors)
            await product.AddVendorAsync(vendorInfo.VendorId, vendorInfo.DisplayOrder, _vendorDataReader).ConfigureAwait(false);

        if (oldCostPrice != product.CostPrice || oldUnitPrice != product.UnitPrice)
        {
            await _priceHistoryRepository.InsertAsync(new ProductPriceHistory(
                product.Id,
                oldUnitPrice,
                product.UnitPrice,
                oldCostPrice,
                product.CostPrice,
                dto.ChangePriceReason ?? "Cập nhật hàng hóa")).ConfigureAwait(false);
        }

        await _productRepository.UpdateAsync(product).ConfigureAwait(false);

        await _eventPublisher.EntityUpdated(product, deletedPictureIds).ConfigureAwait(false);

        return new UpdateProductResultDto(product.Id)
        {
            Name = product.Name,
            ShortDesc = product.ShortDesc,
            UnitMeasurementId = product.UnitMeasurementId,
            UnitPrice = product.UnitPrice,
            CostPrice = product.CostPrice,
            Categories = product.ProductCategories.Select(pc => new ProductCategoryDto(pc.CategoryId, pc.DisplayOrder)),
            Vendors = product.ProductVendors.Select(pv => new ProductVendorDto(pv.VendorId, pv.DisplayOrder)),
            Pictures = product.ProductPictures.Select(pp => pp.PictureId)
        };
    }

    public async Task<IEnumerable<ProductDto>> GetProductsByIdsAsync(IEnumerable<Guid> ids)
    {
        var products = await _productDataReader.GetByIdsAsync(ids).ConfigureAwait(false);
        return products.Select(p => p.ToDto()).ToList();
    }

    public Task<IEnumerable<ProductPriceHistoryDto>> GetProductPriceHistoryAsync(Guid productId)
    {
        return Task.Run(async () =>
        {
            var data = _priceHistoryDataReader.DataSource
                .Where(ph => ph.ProductId == productId)
                .OrderByDescending(ph => ph.CreatedOnUtc)
                .Select(ph => new ProductPriceHistoryDto
                {
                    Id = ph.Id,
                    OldPrice = ph.OldPrice,
                    NewPrice = ph.NewPrice,
                    OldCostPrice = ph.OldCostPrice,
                    NewCostPrice = ph.NewCostPrice,
                    Note = ph.Note,
                    CreatedOnUtc = ph.CreatedOnUtc
                }).ToList();

            return (IEnumerable<ProductPriceHistoryDto>)data;
        });
    }
}
