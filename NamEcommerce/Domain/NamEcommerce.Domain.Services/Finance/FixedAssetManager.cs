using NamEcommerce.Data.Contracts;
using NamEcommerce.Domain.Entities.Finance;
using NamEcommerce.Domain.Services.Common;
using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Domain.Shared.Dtos.Finance;
using NamEcommerce.Domain.Shared.Enums.Finance;
using NamEcommerce.Domain.Shared.Exceptions.Finance;
using NamEcommerce.Domain.Shared.Services.Finance;

namespace NamEcommerce.Domain.Services.Finance;

public sealed class FixedAssetManager : IFixedAssetManager
{
    private readonly IRepository<FixedAsset> _repository;
    private readonly IEntityDataReader<FixedAsset> _dataReader;
    private readonly EntityCodeGenerator _codeGenerator;

    public FixedAssetManager(IRepository<FixedAsset> repository, IEntityDataReader<FixedAsset> dataReader, EntityCodeGenerator codeGenerator)
    {
        _repository = repository;
        _dataReader = dataReader;
        _codeGenerator = codeGenerator;
    }

    public Task<FixedAssetDto?> GetByIdAsync(Guid id)
    {
        var asset = _dataReader.DataSource.FirstOrDefault(a => a.Id == id);
        return Task.FromResult(asset?.ToDto());
    }

    public Task<IReadOnlyList<FixedAssetDto>> GetAllAsync(FixedAssetStatus? status = null)
    {
        var query = _dataReader.DataSource.AsEnumerable();
        if (status.HasValue) query = query.Where(a => a.Status == status.Value);
        IReadOnlyList<FixedAssetDto> result = query
            .OrderByDescending(a => a.CreatedOnUtc)
            .Select(a => a.ToDto())
            .ToList();
        return Task.FromResult(result);
    }

    public async Task<FixedAssetDto> CreateAsync(CreateFixedAssetDto dto)
    {
        dto.Verify();
        var code = await GenerateCodeAsync().ConfigureAwait(false);
        var asset = new FixedAsset(code, dto.Name, dto.Description, dto.Category,
            dto.CostCenter, dto.AcquisitionDate, dto.AcquisitionCost,
            dto.ResidualValue, dto.UsefulLifeMonths,
            dto.VendorId, dto.VendorInvoiceNumber, dto.Note);
        asset.MarkCreated();
        var inserted = await _repository.InsertAsync(asset).ConfigureAwait(false);
        return inserted.ToDto();
    }

    public async Task<FixedAssetDto> UpdateAsync(Guid id, string name, string? description, string? note, FixedAssetCostCenter costCenter)
    {
        var asset = await _repository.GetByIdAsync(id).ConfigureAwait(false)
            ?? throw new FixedAssetNotFoundException(id);
        asset.UpdateInfo(name, description, note, costCenter);
        var updated = await _repository.UpdateAsync(asset).ConfigureAwait(false);
        return updated.ToDto();
    }

    public async Task DisposeAsync(Guid id, DateTime disposedOnUtc)
    {
        var asset = await _repository.GetByIdAsync(id).ConfigureAwait(false)
            ?? throw new FixedAssetNotFoundException(id);
        asset.Dispose(disposedOnUtc);
        await _repository.UpdateAsync(asset).ConfigureAwait(false);
    }

    public Task<string> GenerateCodeAsync()
        => Task.FromResult(_codeGenerator.Next("TSCĐ", () => _dataReader.DataSource.Count()));
}

internal static class FixedAssetExtensions
{
    internal static FixedAssetDto ToDto(this FixedAsset a) => new()
    {
        Id = a.Id,
        Code = a.Code,
        Name = a.Name,
        Description = a.Description,
        Category = a.Category,
        CostCenter = a.CostCenter,
        AcquisitionDate = a.AcquisitionDate,
        AcquisitionCost = a.AcquisitionCost,
        ResidualValue = a.ResidualValue,
        UsefulLifeMonths = a.UsefulLifeMonths,
        MonthlyDepreciation = a.MonthlyDepreciation,
        VendorInvoiceNumber = a.VendorInvoiceNumber,
        Note = a.Note,
        Status = a.Status,
        DisposedOnUtc = a.DisposedOnUtc
    };
}
