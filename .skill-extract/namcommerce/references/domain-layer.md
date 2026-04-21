# Domain Layer — Hướng Dẫn Chi Tiết

## Base Classes

```csharp
// AppAggregateEntity — Aggregate Root có soft delete
public record AppAggregateEntity : AppEntity, ISoftDeletable
{
    public bool IsDeleted { get; private set; }
    public DateTime? DeletedOnUtc { get; private set; }
    public void Delete() { IsDeleted = true; DeletedOnUtc = DateTime.UtcNow; }
}
// AppEntity — base cho child entities
public record AppEntity { public AppEntity(Guid id) { Id = id; } public Guid Id { get; } }
```

---

## Template: Domain Entity

```csharp
// NamEcommerce.Domain/Entities/{Module}/Xyz.cs
[Serializable]
public sealed record Xyz : AppAggregateEntity
{
    internal Xyz(Guid id, string name) : base(id)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        Name = name;
        CreatedOnUtc = DateTime.UtcNow;       // DateTime lưu DB là UTC
    }

    public string Name { get; private set; }
    public DateTime CreatedOnUtc { get; }             // hậu tố Utc
    public DateTime? ProcessedOnUtc { get; private set; } // hậu tố Utc

    internal void SetName(string name)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        Name = name;
    }
    internal void MarkProcessed() => ProcessedOnUtc = DateTime.UtcNow;
}
```

---

## Template: Domain DTOs

```csharp
// NamEcommerce.Domain.Shared/Dtos/{Module}/XyzDtos.cs
[Serializable]
public abstract record BaseXyzDto
{
    public required string Name { get; init; }
    public int DisplayOrder { get; set; }
    public DateTime? FromDateUtc { get; init; }  // hậu tố Utc

    public virtual void Verify()  // throw exception nếu invalid
    {
        if (string.IsNullOrEmpty(Name))
            throw new XyzDataIsInvalidException("Name cannot be empty");
    }
}

[Serializable]
public sealed record XyzDto(Guid Id) : BaseXyzDto
{
    public DateTime CreatedOnUtc { get; init; }  // hậu tố Utc
}

[Serializable] public sealed record CreateXyzDto : BaseXyzDto;
[Serializable] public sealed record CreateXyzResultDto { public required Guid CreatedId { get; init; } }
[Serializable] public sealed record UpdateXyzDto(Guid Id) : BaseXyzDto;
[Serializable] public sealed record UpdateXyzResultDto(Guid Id) : BaseXyzDto;
```

---

## Template: Manager Interface

```csharp
// NamEcommerce.Domain.Shared/Services/{Module}/IXyzManager.cs
public interface IXyzManager
{
    Task<XyzDto?> GetXyzByIdAsync(Guid id);
    Task<IPagedDataDto<XyzDto>> GetXyzsAsync(string? keywords, int pageIndex, int pageSize);
    Task<bool> DoesNameExistAsync(string name, Guid? excludeId = null);
    Task<CreateXyzResultDto> CreateXyzAsync(CreateXyzDto dto);
    Task<UpdateXyzResultDto> UpdateXyzAsync(UpdateXyzDto dto);
    Task DeleteXyzAsync(Guid id);
}
```

---

## Template: Manager Implementation

```csharp
// NamEcommerce.Domain.Services/{Module}/XyzManager.cs
public sealed class XyzManager : IXyzManager
{
    private readonly IRepository<Xyz> _xyzRepository;
    private readonly IEntityDataReader<Xyz> _xyzDataReader;
    private readonly IEventPublisher _eventPublisher;

    public XyzManager(IRepository<Xyz> xyzRepository, IEntityDataReader<Xyz> xyzDataReader,
        IEventPublisher eventPublisher)
    {
        _xyzRepository = xyzRepository;
        _xyzDataReader = xyzDataReader;
        _eventPublisher = eventPublisher;
    }

    public Task<IPagedDataDto<XyzDto>> GetXyzsAsync(string? keywords, int pageIndex, int pageSize)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(pageIndex, 0);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(pageSize, 0);

        var query = _xyzDataReader.DataSource;
        if (!string.IsNullOrEmpty(keywords))
            query = query.Where(x => x.Name.Contains(keywords));
        query = query.OrderBy(x => x.DisplayOrder).ThenBy(x => x.Name);

        var totalCount = query.Count();
        var items = query.Skip(pageIndex * pageSize).Take(pageSize).ToList();
        return Task.FromResult(PagedDataDto.Create(items.Select(x => x.ToDto()), pageIndex, pageSize, totalCount));
    }

    public async Task<CreateXyzResultDto> CreateXyzAsync(CreateXyzDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        dto.Verify();

        if (await DoesNameExistAsync(dto.Name).ConfigureAwait(false))
            throw new XyzNameExistsException(dto.Name);

        var xyz = new Xyz(Guid.NewGuid(), dto.Name) { DisplayOrder = dto.DisplayOrder };
        var inserted = await _xyzRepository.InsertAsync(xyz).ConfigureAwait(false);
        await _eventPublisher.EntityCreated(inserted).ConfigureAwait(false);
        return new CreateXyzResultDto { CreatedId = inserted.Id };
    }

    public async Task<UpdateXyzResultDto> UpdateXyzAsync(UpdateXyzDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        dto.Verify();

        var xyz = await _xyzDataReader.GetByIdAsync(dto.Id).ConfigureAwait(false)
            ?? throw new XyzNotFoundException(dto.Id);

        if (await DoesNameExistAsync(dto.Name, dto.Id).ConfigureAwait(false))
            throw new XyzNameExistsException(dto.Name);

        xyz.SetName(dto.Name);
        xyz.DisplayOrder = dto.DisplayOrder;
        var updated = await _xyzRepository.UpdateAsync(xyz).ConfigureAwait(false);
        await _eventPublisher.EntityUpdated(updated).ConfigureAwait(false);
        return new UpdateXyzResultDto(updated.Id) { Name = dto.Name, DisplayOrder = dto.DisplayOrder };
    }

    public async Task DeleteXyzAsync(Guid id)
    {
        var xyz = await _xyzDataReader.GetByIdAsync(id).ConfigureAwait(false)
            ?? throw new XyzNotFoundException(id);
        await _xyzRepository.DeleteAsync(xyz).ConfigureAwait(false);
        await _eventPublisher.EntityDeleted(xyz).ConfigureAwait(false);
    }

    public Task<bool> DoesNameExistAsync(string name, Guid? excludeId = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        var exists = _xyzDataReader.DataSource
            .Any(x => x.Name == name && (excludeId == null || x.Id != excludeId));
        return Task.FromResult(exists);
    }

    public async Task<XyzDto?> GetXyzByIdAsync(Guid id)
        => (await _xyzDataReader.GetByIdAsync(id))?.ToDto();
}
```

---

## Template: Extension Method ToDto()

```csharp
// NamEcommerce.Domain.Services/Extensions/XyzExtensions.cs
public static class XyzExtensions
{
    public static XyzDto ToDto(this Xyz xyz) => new XyzDto(xyz.Id)
    {
        Name         = xyz.Name,
        DisplayOrder = xyz.DisplayOrder,
        CreatedOnUtc = xyz.CreatedOnUtc   // giữ nguyên UTC
    };
}
```

---

## InternalsVisibleTo

```csharp
// NamEcommerce.Domain/Accessibility/AssemblyAccessibility.cs
[assembly: InternalsVisibleTo("NamEcommerce.Domain.Services")]
[assembly: InternalsVisibleTo("NamEcommerce.Domain.Test")]
```
