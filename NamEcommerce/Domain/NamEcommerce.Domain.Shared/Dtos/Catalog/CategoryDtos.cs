using NamEcommerce.Domain.Shared.Exceptions.Catalog;

namespace NamEcommerce.Domain.Shared.Dtos.Catalog;

[Serializable]
public abstract record BaseCategoryDto
{
    public required string Name { get; init; }
    public int DisplayOrder { get; set; }
    public required Guid? ParentId { get; set; }

    public virtual void Verify()
    {
        if (string.IsNullOrEmpty(Name))
            throw new CategoryDataIsInvalidException("Category name is not empty");
    }
}

[Serializable]
public sealed record CategoryDto(Guid Id) : BaseCategoryDto;

[Serializable]
public sealed record CreateCategoryDto : BaseCategoryDto;
[Serializable]
public sealed record CreateCategoryResultDto
{
    public required Guid CreatedId { get; init; }
}

[Serializable]
public sealed record UpdateCategoryDto(Guid Id) : BaseCategoryDto;
[Serializable]
public sealed record UpdateCategoryResultDto(Guid Id) : BaseCategoryDto;

