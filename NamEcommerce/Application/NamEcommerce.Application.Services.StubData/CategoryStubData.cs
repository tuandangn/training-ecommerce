using NamEcommerce.Application.Contracts.Dtos.Catalog;

namespace NamEcommerce.Application.Services.StubData;

public static class CategoryStubData
{
    static CategoryStubData()
    {
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        IEnumerable<CategoryAppDto> data = new[]
        {
             new CategoryAppDto(id1, "Category test 1"),
             new CategoryAppDto(id2, "Category test 2")
             {
                 ParentId = id1 //category 1 -> category 2
             },
             new CategoryAppDto(Guid.NewGuid(), "Category test 3")
             {
                 ParentId = id2 // category 2 -> category 3
             },
             new CategoryAppDto(Guid.NewGuid(), "Category test 4"),
             new CategoryAppDto(Guid.NewGuid(), "Category test 5")
        };

        Data = data;
    }

    public static IEnumerable<CategoryAppDto> Data { get; set; }
}
