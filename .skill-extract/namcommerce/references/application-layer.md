# Application Layer — Hướng Dẫn Chi Tiết

## Quy tắc quan trọng

- **DateTime:** AppDto dùng hậu tố `Utc` — dữ liệu luôn UTC từ DB lên.
- **Pagination:** Nhận `pageIndex` (0-based) từ Handler/Query — Application không biết về PageNumber.

---

## Template: Application DTOs

```csharp
// NamEcommerce.Application.Contracts/Dtos/{Module}/XyzAppDtos.cs
[Serializable]
public abstract record BaseXyzAppDto
{
    public required string Name { get; init; }
    public int DisplayOrder { get; set; }
    public DateTime? FromDateUtc { get; init; }  // hậu tố Utc

    // Validate() trả về (bool, string?) — KHÔNG throw exception
    public (bool valid, string? errorMessage) Validate()
    {
        if (string.IsNullOrEmpty(Name))
            return (false, "Tên không được để trống");
        return (true, null);
    }
}

[Serializable]
public sealed record XyzAppDto(Guid Id) : BaseXyzAppDto
{
    public DateTime CreatedOnUtc { get; init; }  // hậu tố Utc
}

[Serializable] public sealed record CreateXyzAppDto : BaseXyzAppDto;
[Serializable]
public sealed record CreateXyzResultAppDto
{
    public required bool Success { get; init; }
    public Guid? CreatedId { get; set; }
    public string? ErrorMessage { get; set; }
}

[Serializable] public sealed record UpdateXyzAppDto(Guid Id) : BaseXyzAppDto;
[Serializable]
public sealed record UpdateXyzResultAppDto
{
    public required bool Success { get; init; }
    public Guid UpdatedId { get; set; }
    public string? ErrorMessage { get; set; }
}

[Serializable] public sealed record DeleteXyzAppDto(Guid Id);
[Serializable]
public sealed record DeleteXyzResultAppDto
{
    public required bool Success { get; init; }
    public string? ErrorMessage { get; set; }
}
```

---

## Template: AppService Interface

```csharp
// NamEcommerce.Application.Contracts/{Module}/IXyzAppService.cs
public interface IXyzAppService
{
    // pageIndex là 0-based — Application không nhận PageNumber
    Task<IPagedDataAppDto<XyzAppDto>> GetXyzsAsync(
        string? keywords = null, int pageIndex = 0, int pageSize = int.MaxValue);
    Task<XyzAppDto?> GetXyzByIdAsync(Guid id);
    Task<CreateXyzResultAppDto> CreateXyzAsync(CreateXyzAppDto dto);
    Task<UpdateXyzResultAppDto> UpdateXyzAsync(UpdateXyzAppDto dto);
    Task<DeleteXyzResultAppDto> DeleteXyzAsync(DeleteXyzAppDto dto);
}
```

---

## Template: AppService Implementation

```csharp
// NamEcommerce.Application.Services/{Module}/XyzAppService.cs
public sealed class XyzAppService : IXyzAppService
{
    private readonly IXyzManager _xyzManager;
    private readonly IEntityDataReader<Xyz> _xyzDataReader;

    public XyzAppService(IXyzManager xyzManager, IEntityDataReader<Xyz> xyzDataReader)
    {
        _xyzManager = xyzManager;
        _xyzDataReader = xyzDataReader;
    }

    public async Task<IPagedDataAppDto<XyzAppDto>> GetXyzsAsync(
        string? keywords = null, int pageIndex = 0, int pageSize = int.MaxValue)
    {
        var pagedData = await _xyzManager.GetXyzsAsync(keywords, pageIndex, pageSize).ConfigureAwait(false);
        return PagedDataAppDto.Create(
            pagedData.Select(x => x.ToDto()), pageIndex, pageSize, pagedData.PagerInfo.TotalCount);
    }

    public async Task<XyzAppDto?> GetXyzByIdAsync(Guid id)
        => (await _xyzDataReader.GetByIdAsync(id).ConfigureAwait(false))?.ToDto();

    public async Task<CreateXyzResultAppDto> CreateXyzAsync(CreateXyzAppDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var (valid, errorMessage) = dto.Validate();
        if (!valid) return new CreateXyzResultAppDto { Success = false, ErrorMessage = errorMessage };

        if (await _xyzManager.DoesNameExistAsync(dto.Name).ConfigureAwait(false))
            return new CreateXyzResultAppDto { Success = false, ErrorMessage = "Tên đã tồn tại." };

        var result = await _xyzManager.CreateXyzAsync(new CreateXyzDto
        {
            Name         = dto.Name,
            DisplayOrder = dto.DisplayOrder,
            FromDateUtc  = dto.FromDateUtc   // đã là UTC, truyền thẳng
        }).ConfigureAwait(false);

        return new CreateXyzResultAppDto { Success = true, CreatedId = result.CreatedId };
    }

    public async Task<UpdateXyzResultAppDto> UpdateXyzAsync(UpdateXyzAppDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var (valid, errorMessage) = dto.Validate();
        if (!valid) return new UpdateXyzResultAppDto { Success = false, ErrorMessage = errorMessage };

        var existing = await _xyzDataReader.GetByIdAsync(dto.Id).ConfigureAwait(false);
        if (existing is null)
            return new UpdateXyzResultAppDto { Success = false, ErrorMessage = "Không tìm thấy dữ liệu." };

        if (await _xyzManager.DoesNameExistAsync(dto.Name, dto.Id).ConfigureAwait(false))
            return new UpdateXyzResultAppDto { Success = false, ErrorMessage = "Tên đã tồn tại." };

        var result = await _xyzManager.UpdateXyzAsync(new UpdateXyzDto(dto.Id)
        {
            Name = dto.Name, DisplayOrder = dto.DisplayOrder
        }).ConfigureAwait(false);

        return new UpdateXyzResultAppDto { Success = true, UpdatedId = result.Id };
    }

    public async Task<DeleteXyzResultAppDto> DeleteXyzAsync(DeleteXyzAppDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        var existing = await _xyzDataReader.GetByIdAsync(dto.Id).ConfigureAwait(false);
        if (existing is null)
            return new DeleteXyzResultAppDto { Success = false, ErrorMessage = "Không tìm thấy dữ liệu." };
        await _xyzManager.DeleteXyzAsync(dto.Id).ConfigureAwait(false);
        return new DeleteXyzResultAppDto { Success = true };
    }
}
```

---

## Template: Extension Method (Domain DTO / Entity → App DTO)

```csharp
// NamEcommerce.Application.Services/Extensions/XyzExtensions.cs
public static class XyzExtensions
{
    public static XyzAppDto ToDto(this XyzDto dto) => new XyzAppDto(dto.Id)
    {
        Name         = dto.Name,
        DisplayOrder = dto.DisplayOrder,
        CreatedOnUtc = dto.CreatedOnUtc   // giữ nguyên UTC
    };

    public static XyzAppDto ToDto(this Xyz xyz) => new XyzAppDto(xyz.Id)
    {
        Name         = xyz.Name,
        DisplayOrder = xyz.DisplayOrder,
        CreatedOnUtc = xyz.CreatedOnUtc   // giữ nguyên UTC
    };
}
```
