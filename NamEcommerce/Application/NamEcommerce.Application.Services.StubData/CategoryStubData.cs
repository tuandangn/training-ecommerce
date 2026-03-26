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
             new CategoryAppDto(id1)
             {
                 Name = "Category 1",
                 ParentId = null
             },
             new CategoryAppDto(id2)
             {
                 Name = "Category 2",
                 ParentId = id1 //category 1 -> category 2
             },
             new CategoryAppDto(Guid.NewGuid())
             {
                 Name = "Category 3",
                 ParentId = id2 // category 2 -> category 3
             },
             new CategoryAppDto(Guid.NewGuid()){
                 Name = "Category 4",
                 ParentId = null
             },
             new CategoryAppDto(Guid.NewGuid())
             {
                 Name = "Category 5",
                 ParentId = null
             }
        };

        Data = data;
    }

    public static IEnumerable<CategoryAppDto> Data { get; set; }
}
