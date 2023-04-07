using NamEcommerce.Application.Shared.Dtos.Catalog;

namespace NamEcommerce.Application.Services.StubData;

public static class CategoryStubData
{
    static CategoryStubData()
    {
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        IEnumerable<CategoryDto> data = new[]
        {
             new CategoryDto(id1, "Category test 1"),
             new CategoryDto(id2, "Category test 2")
             {
                 ParentId = id1 //category 1 -> category 2
             },
             new CategoryDto(Guid.NewGuid(), "Category test 3")
             {
                 ParentId = id2 // category 2 -> category 3
             },
             new CategoryDto(Guid.NewGuid(), "Category test 4"),
             new CategoryDto(Guid.NewGuid(), "Category test 5")
        };

        Data = data;
    }

    public static IEnumerable<CategoryDto> Data { get; set; }
}
