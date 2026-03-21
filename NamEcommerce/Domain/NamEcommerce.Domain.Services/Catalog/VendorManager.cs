using NamEcommerce.Data.Contracts;
using NamEcommerce.Domain.Entities.Catalog;
using NamEcommerce.Domain.Services.Extensions;
using NamEcommerce.Domain.Shared.Dtos.Catalog;
using NamEcommerce.Domain.Shared.Dtos.Common;
using NamEcommerce.Domain.Shared.Exceptions.Catalog;
using NamEcommerce.Domain.Shared.Helpers;
using NamEcommerce.Domain.Shared.Services;
using NamEcommerce.Domain.Shared.Services.Catalog;

namespace NamEcommerce.Domain.Services.Catalog;

public sealed class VendorManager : IVendorManager
{
    private readonly IRepository<Vendor> _vendorRepository;
    private readonly IEntityDataReader<Vendor> _vendorDataReader;

    public VendorManager(IRepository<Vendor> vendorRepository, IEntityDataReader<Vendor> vendorEntityDataReader)
    {
        _vendorRepository = vendorRepository;
        _vendorDataReader = vendorEntityDataReader;
    }

    public async Task<CreateVendorResultDto> CreateVendorAsync(CreateVendorDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        dto.Verify();

        if (await DoesNameExistAsync(dto.Name).ConfigureAwait(false))
            throw new VendorNameExistsException(dto.Name);

        var insertedVendor = await _vendorRepository.InsertAsync(new Vendor(Guid.NewGuid(), dto.Name, dto.PhoneNumber)
        {
            Address = dto.Address,
            DisplayOrder = dto.DisplayOrder,
        }).ConfigureAwait(false);

        return new CreateVendorResultDto
        {
            CreatedId = insertedVendor.Id
        };
    }

    public async Task DeleteVendorAsync(Guid id)
    {
        var vendor = await _vendorDataReader.GetByIdAsync(id).ConfigureAwait(false);
        if (vendor is null)
            throw new ArgumentException("Vendor is not found", nameof(id));

        await _vendorRepository.DeleteAsync(vendor).ConfigureAwait(false);
    }

    public Task<bool> DoesNameExistAsync(string name, Guid? comparesWithCurrentId = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        var query = from vendor in _vendorDataReader.DataSource
                    where vendor.Name == name && (comparesWithCurrentId == null || vendor.Id != comparesWithCurrentId)
                    select vendor;

        var sameNameExists = query.FirstOrDefault() != null;
        return Task.FromResult(sameNameExists);
    }

    public Task<IPagedDataDto<VendorDto>> GetVendorsAsync(string? keywords, int pageIndex, int pageSize)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(pageIndex, 0, nameof(pageIndex));
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(pageSize, 0, nameof(pageSize));

        var query = _vendorDataReader.DataSource;

        if (!string.IsNullOrEmpty(keywords))
        {
            var normizedKeywords = TextHelper.Normalize(keywords);
            query = query.Where(c => c.NormalizedName.Contains(normizedKeywords));
        }

        query = query.OrderBy(c => c.DisplayOrder)
            .ThenBy(c => c.Name);

        var totalCount = query.Count();
        var pagedData = query
            .Skip(pageIndex * pageSize)
            .Take(pageSize)
            .ToList();

        var data = PagedDataDto.Create(pagedData.Select(vendor => vendor.ToDto()), pageIndex, pageSize, totalCount);
        return Task.FromResult(data);
    }

    public Task<UpdateVendorResultDto> UpdateVendorAsync(UpdateVendorDto dto)
    {
        throw new NotImplementedException();
    }
}
