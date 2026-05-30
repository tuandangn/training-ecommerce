# Application Layer — Chi tiết

## Naming Conventions

| Item | Convention | Ví dụ |
|---|---|---|
| Interface | `I{Entity}AppService` | `ICategoryAppService` |
| Implementation | `{Entity}AppService` | `CategoryAppService` |
| Input DTO | `Create{Entity}AppDto` | `CreateCategoryAppDto` |
| Output DTO | `{Entity}AppDto`, `Create{Entity}ResultAppDto` | `CategoryAppDto`, `CreateCategoryResultAppDto` |
| DTO location | `Application.Contracts/Dtos/{Module}/` | |
| Interface location | `Application.Contracts/{Module}/` | |
| Implementation location | `Application.Services/{Module}/` | |
| Extension ToDto() | `Application.Services/Extensions/` | |

---

## Template: Application DTOs

```csharp
// NamEcommerce.Application.Contracts/Dtos/{Module}/XyzAppDtos.cs
namespace NamEcommerce.Application.Contracts.Dtos.{Module};

[Serializable]
public abstract record BaseXyzAppDto// contains only necessary information for base create/update/read Domain dto
{
    public required string Name { get; init; }
    public string? Description { get; init; }
    public int DisplayOrder { get; set; }

    // Validate() RETURN (bool, string?), KHÔNG throw exception
    public virtual (bool valid, string? errorMessage) Validate()
    {
        if (string.IsNullOrEmpty(Name))
            return (false, "Tên không được để trống");
        return (true, null);
    }
}

[Serializable] public sealed record XyzAppDto(Guid Id) : BaseXyzAppDto {
    // additional information for presentation use
}
[Serializable] public sealed record CreateXyzAppDto : BaseXyzAppDto {
    // only necessary information for create Create domain dto

    // override Validate if necessary
    public override (bool valid, string? errorMessage) Validate()
    {
    }
}
[Serializable] public sealed record UpdateXyzAppDto(Guid Id) : BaseXyzAppDto {
    // only necessary information for create Update domain dto

    // override Validate if necessary
    public override (bool valid, string? errorMessage) Validate()
    {
    }
}

[Serializable]
public sealed record CreateXyzResultAppDto
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public Guid? CreatedId { get; init; }
}

[Serializable]
public sealed record UpdateXyzResultAppDto
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public Guid? UpdatedId { get; init; }
}
```

---

## Template: AppService Interface

```csharp
// NamEcommerce.Application.Contracts/{Module}/IXyzAppService.cs
namespace NamEcommerce.Application.Contracts.{Module};

public interface IXyzAppService
{
    Task<XyzAppDto?> GetXyzByIdAsync(Guid id);
    Task<IPagedDataDto<XyzAppDto>> GetXyzsAsync(string? keywords, int pageIndex, int pageSize);
    Task<CreateXyzResultAppDto> CreateXyzAsync(CreateXyzAppDto dto); // ALWAYS use AppDtos for commands (excepts Delete)
    Task<UpdateXyzResultAppDto> UpdateXyzAsync(UpdateXyzAppDto dto);
    Task<bool> DeleteXyzAsync(Guid id);
}
```

---

## Template: AppService Implementation

```csharp
// NamEcommerce.Application.Services/{Module}/XyzAppService.cs
namespace NamEcommerce.Application.Services.{Module};

public sealed class XyzAppService : IXyzAppService
{
    private readonly IXyzManager _xyzManager; // use domain server IXxxManager when write, IEntityDataReader for read
    private readonly IEntityDataReader<Yyy> _yyyDataReader; // use domain server IXxxManager when write, IEntityDataReader for read

    public XyzAppService(IXyzManager xyzManager, IEntityDataReader<Yyy> yyyDataReader)
    {
        _xyzManager = xyzManager;
        _yyyDataReader = yyyDataReader;
    }

    public Task<XyzAppDto?> GetXyzByIdAsync(Guid id)
        => _xyzManager.GetXyzByIdAsync(id)
            .ContinueWith(t => t.Result?.ToDto());

    public async Task<IPagedDataDto<XyzAppDto>> GetXyzsAsync(string? keywords, int pageIndex, int pageSize)
    {
        var result = await _xyzManager.GetXyzsAsync(keywords, pageIndex, pageSize);
        return PagedDataDto.Create(result.Data.Select(x => x.ToDto()), pageIndex, pageSize, result.TotalCount);
    }

    public async Task<CreateXyzResultAppDto> CreateXyzAsync(CreateXyzAppDto dto)
    {
        var (valid, errorMessage) = dto.Validate();
        if (!valid) return new CreateXyzResultAppDto { Success = false, ErrorMessage = errorMessage };

        // try check entity existing early
        // validates dto values early

        if (await _xyzManager.DoesNameExistAsync(dto.Name).ConfigureAwait(false))
        {
            return new CreateXyzResultAppDto
            {
                Success = false,
                ErrorMessage = "Error.XyzNameAlreadyExists"
            };
        }

        if (dto.YyyId.HasValue)
        {
            var yyy = await _yyyDataReader.GetByIdAsync(dto.YyyId.Value).ConfigureAwait(false);
            if (yyy is null)
            {
                return new CreateXyzResultAppDto
                {
                    Success = false,
                    ErrorMessage = "Error.YyyIsNotFound"
                };
            }
        }
        //addition value checking before call domain service xyzManager method
        //ONLY use try catch when necessary

        var result = await _xyzManager.CreateXyzAsync(dto.ToDomainDto());
        return new CreateXyzResultAppDto { Success = true, CreatedId = result.CreatedId };
    }

    public async Task<UpdateXyzResultAppDto> UpdateXyzAsync(UpdateXyzAppDto dto)
    {
        var (valid, errorMessage) = dto.Validate();
        if (!valid) return new UpdateXyzResultAppDto { Success = false, ErrorMessage = errorMessage };

        // try check entity existing early
        // validates dto values early
        
        if (await _xyzManager.DoesNameExistAsync(dto.Name).ConfigureAwait(false))
        {
            return new CreateXyzResultAppDto
            {
                Success = false,
                ErrorMessage = "Error.XyzNameAlreadyExists"
            };
        }

        if (dto.YyyId.HasValue)
        {
            var yyy = await _yyyDataReader.GetByIdAsync(dto.YyyId.Value).ConfigureAwait(false);
            if (yyy is null)
            {
                return new CreateXyzResultAppDto
                {
                    Success = false,
                    ErrorMessage = "Error.YyyIsNotFound"
                };
            }
        }

        //addition value checking before call domain service xyzManager methodx
        //ONLY use try catch when necessary
        var result = await _xyzManager.UpdateXyzAsync(dto.ToDomainDto());
        return new UpdateXyzResultAppDto { Success = true, UpdatedId = result.Id };
    }

    public async Task<bool> DeleteXyzAsync(Guid id)
    {
        await _xyzManager.DeleteXyzAsync(id);
        return true;
    }
}
```

---

## Template: Extension (domain DTO → app DTO)

```csharp
// NamEcommerce.Application.Services/Extensions/XyzExtensions.cs
namespace NamEcommerce.Application.Services.Extensions;

public static class XyzExtensions
{
    // Domain DTO → App DTO
    public static XyzAppDto ToDto(this XyzDto xyz) => new(xyz.Id)
    {
        Name = xyz.Name,
        Description = xyz.Description,
        DisplayOrder = xyz.DisplayOrder
    };
}
```

---

## Unit Test Template

```csharp
public sealed class XyzAppServiceTests
{
    [Fact]
    public async Task CreateXyzAsync_InvalidName_ReturnsFalse()
    {
        var service = new XyzAppService(null!);
        var result = await service.CreateXyzAsync(new CreateXyzAppDto { Name = "" });
        Assert.False(result.Success);
        Assert.NotEmpty(result.ErrorMessage!);
    }

    [Fact]
    public async Task CreateXyzAsync_NameExists_ReturnsFalse()
    {
        var managerMock = new Mock<IXyzManager>();
        managerMock.Setup(m => m.CreateXyzAsync(It.IsAny<CreateXyzDto>()))
            .ThrowsAsync(new XyzNameExistsException("existing"));

        var service = new XyzAppService(managerMock.Object);
        var result = await service.CreateXyzAsync(new CreateXyzAppDto { Name = "existing" });
        Assert.False(result.Success);
    }
}
```
