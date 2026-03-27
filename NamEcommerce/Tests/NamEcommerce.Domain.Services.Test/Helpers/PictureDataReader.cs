using NamEcommerce.Domain.Entities.Media;
using NamEcommerce.Domain.Shared.Common;

namespace NamEcommerce.Domain.Services.Test.Helpers;

public static class PictureDataReader
{
    public static Mock<IEntityDataReader<Picture>> NotFound(Guid notFoundId)
    {
        var mock = EntityDataReader.Create<Picture>();
        mock.Setup(dataReader => dataReader.GetByIdAsync(notFoundId)).ReturnsAsync((Picture?)null);
        return mock;
    }

    public static Mock<IEntityDataReader<Picture>> PictureById(Guid id, Picture @return)
    {
        var mock = EntityDataReader.Create<Picture>();
        mock.Setup(dataReader => dataReader.GetByIdAsync(id)).ReturnsAsync(@return);
        return mock;
    }
}
