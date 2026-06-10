# Domain Layer — Chi tiết

## Base Classes

```csharp
// AppEntity — base cho tất cả entities
public record AppEntity { public AppEntity(Guid id) { Id = id; } public Guid Id { get; } }

// AppAggregateEntity — có soft delete (dùng cho Aggregate Root)
public record AppAggregateEntity : AppEntity, ISoftDeletable
{
    public bool IsDeleted { get; private set; }
    public DateTime? DeletedOnUtc { get; private set; }
    public void Delete() { IsDeleted = true; DeletedOnUtc = DateTime.UtcNow; }
}
```

---

## Template: Domain Entity

```csharp
// NamEcommerce.Domain/Entities/{Module}/Xyz.cs
namespace NamEcommerce.Domain.Entities.{Module};

[Serializable]
public sealed record Xyz : AppAggregateEntity
{
    // Constructor internal — chỉ Manager mới được tạo
    internal Xyz(Guid id, string name) : base(id)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        Name = name;
        CreatedOnUtc = DateTime.UtcNow;
    }

    public NormalizableString Name { get; private set; } // use NormalizableString for searchable fields
    public NormalizableString Description { get; internal set; } // internal set OK cho simple props
    public int DisplayOrder { get; internal set; }
    public DateTime CreatedOnUtc { get; }

    // Methods thay đổi state: internal
    internal async Task SetName(string name, INameExistCheckingService checker) // very simple checking service in NamEcommerce.Domain.Shared/Common
    {
        if (string.Equals(Name, name, StringComparison.Ordinal))
            return;

        ArgumentNullException.ThrowIfNull(checker);
        if (string.IsNullOrEmpty(name))
            throw new xxxNameRequiredException(); // throw domain exception

        if (await checker.DoesNameExistAsync(name, Id).ConfigureAwait(false))
            throw new xxxNameExistsException(name);

        var oldName = Name;
        Name = name;
        NameChanged(oldName, Name); //fire domain events when IMPORTANT fields changed 
    }

    // Domain events for cross entites updating
    internal void MarkCreated() => RaiseDomainEvent(new xxxCreated(Id)); // internal for Domain Service calls
    private void NameChanged(string oldName, string newName) => RaiseDomainEvent(new xxxNameChanged(oldName, newName));
}
```

---

## Template: Domain DTOs

```csharp
// NamEcommerce.Domain.Shared/Dtos/{Module}/XyzDtos.cs
namespace NamEcommerce.Domain.Shared.Dtos.{Module};

[Serializable]
public abstract record BaseXyzDto // contains only necessary information for base create/update/read entity
{
    public required string Name { get; init; }
    public string? Description { get; init; }
    public int DisplayOrder { get; set; }

    // Verify() THROW exception, KHÔNG return bool
    public virtual void Verify()
    {
        if (string.IsNullOrEmpty(Name))
            throw new XyzDataIsInvalidException("Tên không được để trống");
    }
}

[Serializable] public sealed record XyzDto(Guid Id) : BaseXyzDto{ 
    // additional information for application use
}
[Serializable] public sealed record CreateXyzDto : BaseXyzDto {
    // only necessary information for create entity

    // override Veriry if necessary
    public override void Verify()
    {
    }
}
[Serializable] public sealed record CreateXyzResultDto { public required Guid CreatedId { get; init; } }
[Serializable] public sealed record UpdateXyzDto(Guid Id) : BaseXyzDto {
    // only necessary information for update entity

    // override Veriry if necessary
    public override void Verify()
    {
    }
}
[Serializable] public sealed record UpdateXyzResultDto(Guid Id) : BaseXyzDto;
```

---

## Template: Manager Interface

```csharp
// NamEcommerce.Domain.Shared/Services/{Module}/IXyzManager.cs
namespace NamEcommerce.Domain.Shared.Services.{Module};

public interface IXyzManager
{
    Task<XyzDto?> GetXyzByIdAsync(Guid id);
    Task<IPagedDataDto<XyzDto>> GetXyzsAsync(string? keywords, int pageIndex, int pageSize);
    Task<bool> DoesNameExistAsync(string name, Guid? excludeId = null);
    Task<CreateXyzResultDto> CreateXyzAsync(CreateXyzDto dto); // ALWAYS use DTOs for commands (excepts Delete)
    Task<UpdateXyzResultDto> UpdateXyzAsync(UpdateXyzDto dto);
    Task DeleteXyzAsync(Guid id);
}
```

---

## Template: Manager Implementation

```csharp
// NamEcommerce.Domain.Services/{Module}/XyzManager.cs
namespace NamEcommerce.Domain.Services.{Module};

public sealed class XyzManager : IXyzManager
{
    private readonly IRepository<Xyz> _xyzRepository;
    private readonly IEntityDataReader<Xyz> _xyzDataReader;
    private readonly IEventPublisher _eventPublisher;

    public XyzManager(IRepository<Xyz> xyzRepository, IEntityDataReader<Xyz> xyzDataReader, IEventPublisher eventPublisher)
    {
        _xyzRepository = xyzRepository;
        _xyzDataReader = xyzDataReader;
        _eventPublisher = eventPublisher;
    }

    public async Task<XyzDto?> GetXyzByIdAsync(Guid id)
        => (await _xyzDataReader.GetByIdAsync(id))?.ToDto();

    public async Task<CreateXyzResultDto> CreateXyzAsync(CreateXyzDto dto)  //ALWAYS use DTOs for methods has many parameters
    {
        ArgumentNullException.ThrowIfNull(dto);

        dto.Verify();

        if (await DoesNameExistAsync(dto.Name, null).ConfigureAwait(false)) // check name not exists
            throw new XxxNameExistsException(dto.Name); //throw domain exception

        var xxx = new Xxxx(Guid.NewGuid(), dto.Name)
        {
            DisplayOrder = dto.DisplayOrder
        };
        await xxx.SetXxxAsync(dto.XxxId, _xxxDataReader).ConfigureAwait(false); // check refereneced entity is existing
        xxx.MarkCreated(); // ALWAYS use domain event for cross entity updating

        var insertedXxx = await _xxxRepository.InsertAsync(xxx).ConfigureAwait(false);

        return new CreateXyzResultDto
        {
            CreatedId = insertedXxx.Id,
            // other necessary information
        };
    }

    public async Task<UpdateXyzResultDto> UpdateXyzAsync(UpdateXyzDto dto) //ALWAYS use DTOs for methods has many parameters
    {
        ArgumentNullException.ThrowIfNull(dto);

        dto.Verify();

        var xxx = await _xxxDataReader.GetByIdAsync(dto.Id).ConfigureAwait(false);
        if (xxx is null)
            throw new XxxIsNotFoundException(dto.Id);

        var referencedYyy = await _yyyDataReader.GetByIdAsync(dto.YyyId).ConfigureAwait(false);
        if (referencedYyy is null)
            throw new YyyIsNotFoundException(dto.YyyId);

        if (dto.ZzzValue < validZzzValue) // check value validity
            throw new XxxDataIsInvalidException("Error.ZzzValueIsInvalidData");

        xxx.ZzzValue = dto.ZzzValue;
        await purchaseOrder.ChangeYyyAsync(dto.YyyId, _yyyDataReader).ConfigureAwait(false);

        xxx.MarkUpdated();
        var updatedXxx = await _xxxRepository.UpdateAsync(xxx).ConfigureAwait(false);

        return new UpdateXxxResultDto(updatedXxx.Id)
        {
            PlacedOnUtc = updatedXxx.PlacedOnUtc,
            YyyId = updatedXxx.YyyId,
            ZzzValue = updatedXxx.ZzzValue,
            //other necessary information
        };

    }

    public async Task DeleteXyzAsync(Guid id)
    {
        var xyz = await _xyzDataReader.GetByIdAsync(id)
            ?? throw new XyzNotFoundException(id);
        xyz.MarkDeleted();
        await _eventPublisher.EntityDeleted(xyz);
    }

    public Task<IPagedDataDto<XyzDto>> GetXyzsAsync(string? keywords, int pageIndex, int pageSize)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(pageIndex, 0);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(pageSize, 0);

        var query = _xyzDataReader.DataSource;
        if (!string.IsNullOrEmpty(keywords))
            query = query.Where(x => x.Name.Contains(keywords));

        query = query.OrderBy(x => x.DisplayOrder).ThenBy(x => x.Name);
        var total = query.Count();
        var data = query.Skip(pageIndex * pageSize).Take(pageSize).ToList();
        return Task.FromResult(PagedDataDto.Create(data.Select(x => x.ToDto()), pageIndex, pageSize, total));
    }

    public Task<bool> DoesNameExistAsync(string name, Guid? excludeId = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        var exists = _xyzDataReader.DataSource.Any(x => x.Name == name && (excludeId == null || x.Id != excludeId));
        return Task.FromResult(exists);
    }

}
```

---

## Template: Extension ToDto()

```csharp
// NamEcommerce.Domain.Services/Extensions/XyzExtensions.cs
namespace NamEcommerce.Domain.Services.Extensions;

public static class XyzExtensions
{
    public static XyzDto ToDto(this Xyz xyz) => new(xyz.Id)
    {
        Name = xyz.Name,
        Description = xyz.Description,
        DisplayOrder = xyz.DisplayOrder
    };
}
```

---

## InternalsVisibleTo - ONLY NECESSARY

```csharp
// NamEcommerce.Domain/Accessibility/AssemblyAccessibility.cs
[assembly: InternalsVisibleTo("NamEcommerce.Domain.Services")]
[assembly: InternalsVisibleTo("NamEcommerce.Domain.Test")]
```

## IEntityDataReader và IRepository — tóm tắt

`IEntityDataReader<T>` chỉ trả về **untracked** entity — dùng cho read-only.  
`IRepository<T>` stage thay đổi (không gọi SaveChanges bên trong) — `UnitOfWorkBehavior` commit cuối mỗi Command.

```csharp
// Read-only — tất cả đều untracked (AsNoTracking)
await _xyzDataReader.GetByIdAsync(id);
await _xyzDataReader.GetAllAsync();
_xyzDataReader.DataSource.Where(...).OrderBy(...).ToList();

// Tracked load — dùng khi cần concurrency rowversion hoặc identity-map guarantee
await _xyzRepository.GetByIdAsync(id);

// Write — stage thay đổi, không save ngay; commit xảy ra cuối pipeline
await _xyzRepository.InsertAsync(xyz);
await _xyzRepository.UpdateAsync(xyz);   // handle cả tracked lẫn detached entity
await _xyzRepository.DeleteAsync(xyz);

// Pattern sửa entity:
var xyz = await _xyzDataReader.GetByIdAsync(id) ?? throw new XyzNotFoundException(id);
xyz.SomeMutation();                          // mutate
await _xyzRepository.UpdateAsync(xyz);       // StagedRepository tự attach nếu detached
```
