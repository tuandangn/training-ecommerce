using NamEcommerce.Application.Contracts.Dtos.Finance;
using NamEcommerce.Application.Contracts.Finance;
using NamEcommerce.Domain.Shared.Dtos.Finance;
using NamEcommerce.Domain.Shared.Enums.Finance;
using NamEcommerce.Domain.Shared.Exceptions.Finance;
using NamEcommerce.Domain.Shared.Services.Finance;

namespace NamEcommerce.Application.Services.Finance;

public sealed class FixedAssetAppService(IFixedAssetManager manager) : IFixedAssetAppService
{
    public async Task<IReadOnlyList<FixedAssetAppDto>> GetFixedAssetsAsync(int? status = null)
    {
        var now = DateTime.UtcNow;
        var statusEnum = status.HasValue ? (FixedAssetStatus?)status.Value : null;
        var dtos = await manager.GetAllAsync(statusEnum).ConfigureAwait(false);
        return dtos.Select(d => ToAppDto(d, now)).ToList();
    }

    public async Task<FixedAssetAppDto?> GetFixedAssetByIdAsync(Guid id)
    {
        var dto = await manager.GetByIdAsync(id).ConfigureAwait(false);
        return dto is null ? null : ToAppDto(dto, DateTime.UtcNow);
    }

    public async Task<IReadOnlyList<DepreciationScheduleItemAppDto>> GetDepreciationScheduleAsync(Guid id)
    {
        var dto = await manager.GetByIdAsync(id).ConfigureAwait(false);
        if (dto is null) return [];

        var start = dto.AcquisitionDate.Day == 1
            ? new DateTime(dto.AcquisitionDate.Year, dto.AcquisitionDate.Month, 1)
            : new DateTime(dto.AcquisitionDate.Year, dto.AcquisitionDate.Month, 1).AddMonths(1);
        var monthly = Math.Round(dto.MonthlyDepreciation, 0);
        decimal cumulative = 0;
        var result = new List<DepreciationScheduleItemAppDto>(dto.UsefulLifeMonths);

        for (int i = 0; i < dto.UsefulLifeMonths; i++)
        {
            var current = start.AddMonths(i);
            cumulative += monthly;
            result.Add(new DepreciationScheduleItemAppDto
            {
                Year = current.Year,
                Month = current.Month,
                DepreciationAmount = monthly,
                CumulativeDepreciation = cumulative,
                BookValue = dto.AcquisitionCost - cumulative
            });
        }
        return result;
    }

    public async Task<FixedAssetOperationResultAppDto> CreateFixedAssetAsync(CreateFixedAssetAppDto dto)
    {
        var (valid, error) = dto.Validate();
        if (!valid) return Fail(error);

        try
        {
            var created = await manager.CreateAsync(new CreateFixedAssetDto
            {
                Name = dto.Name,
                Description = dto.Description,
                Category = (FixedAssetCategory)dto.Category,
                CostCenter = (FixedAssetCostCenter)dto.CostCenter,
                AcquisitionDate = dto.AcquisitionDate,
                AcquisitionCost = dto.AcquisitionCost,
                ResidualValue = dto.ResidualValue,
                UsefulLifeMonths = dto.UsefulLifeMonths,
                VendorInvoiceNumber = dto.VendorInvoiceNumber,
                Note = dto.Note
            }).ConfigureAwait(false);
            return new FixedAssetOperationResultAppDto { Success = true, AssetId = created.Id };
        }
        catch (FixedAssetDataInvalidException ex) { return Fail(ex.Message); }
    }

    public async Task<FixedAssetOperationResultAppDto> UpdateFixedAssetAsync(Guid id, string name, string? description, string? note, int costCenter)
    {
        try
        {
            await manager.UpdateAsync(id, name, description, note, (FixedAssetCostCenter)costCenter).ConfigureAwait(false);
            return Ok();
        }
        catch (FixedAssetNotFoundException ex) { return Fail(ex.Message); }
        catch (FixedAssetDataInvalidException ex) { return Fail(ex.Message); }
    }

    public async Task<FixedAssetOperationResultAppDto> DisposeFixedAssetAsync(Guid id, DateTime disposedOnUtc)
    {
        try
        {
            await manager.DisposeAsync(id, disposedOnUtc).ConfigureAwait(false);
            return Ok();
        }
        catch (FixedAssetNotFoundException ex) { return Fail(ex.Message); }
        catch (FixedAssetDataInvalidException ex) { return Fail(ex.Message); }
    }

    private static FixedAssetAppDto ToAppDto(FixedAssetDto d, DateTime asOf)
    {
        var depStartDate = d.AcquisitionDate.Day == 1
            ? new DateTime(d.AcquisitionDate.Year, d.AcquisitionDate.Month, 1)
            : new DateTime(d.AcquisitionDate.Year, d.AcquisitionDate.Month, 1).AddMonths(1);

        var effectiveAsOf = d.Status == FixedAssetStatus.Disposed && d.DisposedOnUtc.HasValue
            ? d.DisposedOnUtc.Value
            : asOf;

        int elapsed = effectiveAsOf < depStartDate ? 0
            : Math.Min((effectiveAsOf.Year - depStartDate.Year) * 12
                + (effectiveAsOf.Month - depStartDate.Month) + 1, d.UsefulLifeMonths);

        var accumulated = Math.Round(d.MonthlyDepreciation * elapsed, 0);

        return new FixedAssetAppDto
        {
            Id = d.Id,
            Code = d.Code,
            Name = d.Name,
            Description = d.Description,
            Category = (int)d.Category,
            CategoryDisplay = d.Category switch
            {
                FixedAssetCategory.Vehicle => "Phương tiện vận chuyển",
                FixedAssetCategory.Equipment => "Máy móc thiết bị",
                FixedAssetCategory.FurnitureAndFixtures => "Dụng cụ nội thất",
                FixedAssetCategory.Computer => "Máy tính & thiết bị số",
                _ => "Khác"
            },
            CostCenter = (int)d.CostCenter,
            AcquisitionDate = d.AcquisitionDate,
            AcquisitionCost = d.AcquisitionCost,
            ResidualValue = d.ResidualValue,
            UsefulLifeMonths = d.UsefulLifeMonths,
            MonthlyDepreciation = d.MonthlyDepreciation,
            AccumulatedDepreciation = accumulated,
            BookValue = d.AcquisitionCost - accumulated,
            RemainingMonths = Math.Max(d.UsefulLifeMonths - elapsed, 0),
            Status = (int)d.Status,
            DisposedOnUtc = d.DisposedOnUtc
        };
    }

    private static FixedAssetOperationResultAppDto Ok() => new() { Success = true };
    private static FixedAssetOperationResultAppDto Fail(string? msg) => new() { Success = false, ErrorMessage = msg };
}
