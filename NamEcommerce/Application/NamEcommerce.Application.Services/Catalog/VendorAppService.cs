using NamEcommerce.Application.Contracts.Catalog;
using NamEcommerce.Application.Contracts.Dtos.Catalog;
using NamEcommerce.Application.Contracts.Dtos.Common;
using NamEcommerce.Application.Services.Extensions;
using NamEcommerce.Domain.Entities.Catalog;
using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Domain.Shared.Dtos.Catalog;
using NamEcommerce.Domain.Shared.Services.Catalog;

namespace NamEcommerce.Application.Services.Catalog;

public sealed class VendorAppService : IVendorAppService
{
    private readonly IVendorManager _vendorManager;
    private readonly IEntityDataReader<Vendor> _vendorDataReader;

    public VendorAppService(IVendorManager vendorManager, IEntityDataReader<Vendor> vendorDataReader)
    {
        _vendorManager = vendorManager;
        _vendorDataReader = vendorDataReader;
    }

    public async Task<CreateVendorResultAppDto> CreateVendorAsync(CreateVendorAppDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var (valid, errorMessage) = dto.Validate();
        if (!valid)
        {
            return new CreateVendorResultAppDto
            {
                Success = false,
                ErrorMessage = errorMessage
            };
        }

        if (await _vendorManager.DoesNameExistAsync(dto.Name).ConfigureAwait(false))
        {
            return new CreateVendorResultAppDto
            {
                Success = false,
                ErrorMessage = "Name already exists."
            };
        }

        var result = await _vendorManager.CreateVendorAsync(new CreateVendorDto
        {
            Name = dto.Name,
            PhoneNumber = dto.PhoneNumber,
            Address = dto.Address,
            DisplayOrder = dto.DisplayOrder
        }).ConfigureAwait(false);

        return new CreateVendorResultAppDto
        {
            Success = true,
            CreatedId = result.CreatedId
        };
    }

    public async Task<DeleteVendorResultAppDto> DeleteVendorAsync(DeleteVendorAppDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var vendor = await _vendorDataReader.GetByIdAsync(dto.Id).ConfigureAwait(false);
        if (vendor == null)
        {
            return new DeleteVendorResultAppDto
            {
                Success = false,
                ErrorMessage = "Vendor is not found."
            };
        }

        await _vendorManager.DeleteVendorAsync(dto.Id).ConfigureAwait(false);

        return new DeleteVendorResultAppDto { Success = true };
    }

    public async Task<VendorAppDto?> GetVendorByIdAsync(Guid id)
    {
        var vendor = await _vendorDataReader.GetByIdAsync(id).ConfigureAwait(false);
        return vendor?.ToDto();
    }

    public async Task<IEnumerable<VendorAppDto>> GetVendorsByIdsAsync(IEnumerable<Guid> ids)
    {
        var vendors = await _vendorDataReader.GetByIdsAsync(ids).ConfigureAwait(false);
        return vendors.Select(vendor => vendor.ToDto());
    }

    public async Task<IPagedDataAppDto<VendorAppDto>> GetVendorsAsync(string? keywords = null, int pageIndex = 0, int pageSize = int.MaxValue)
    {
        var pagedData = await _vendorManager.GetVendorsAsync(keywords, pageIndex, pageSize).ConfigureAwait(false);
        var result = PagedDataAppDto.Create(
            pagedData.Select(vendor => vendor.ToDto()),
            pageIndex, pageSize, pagedData.PagerInfo.TotalCount);

        return result;
    }

    public async Task<UpdateVendorResultAppDto> UpdateVendorAsync(UpdateVendorAppDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var (valid, errorMessage) = dto.Validate();
        if (!valid)
        {
            return new UpdateVendorResultAppDto
            {
                Success = false,
                ErrorMessage = errorMessage
            };
        }

        var vendor = await _vendorDataReader.GetByIdAsync(dto.Id).ConfigureAwait(false);
        if (vendor == null)
        {
            return new UpdateVendorResultAppDto
            {
                Success = false,
                ErrorMessage = "Không tìm thấy nhà cung cấp"
            };
        }

        if (await _vendorManager.DoesNameExistAsync(dto.Name, dto.Id).ConfigureAwait(false))
        {
            return new UpdateVendorResultAppDto
            {
                Success = false,
                ErrorMessage = "Tên nhà cung cấp trùng lặp"
            };
        }

        var result = await _vendorManager.UpdateVendorAsync(new UpdateVendorDto(dto.Id)
        {
            Name = dto.Name,
            PhoneNumber = dto.PhoneNumber,
            Address = dto.Address,
            DisplayOrder = dto.DisplayOrder
        }).ConfigureAwait(false);

        return new UpdateVendorResultAppDto
        {
            Success = true,
            UpdatedId = result.Id
        };
    }
}
