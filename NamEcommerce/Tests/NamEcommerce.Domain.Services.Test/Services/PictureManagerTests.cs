using NamEcommerce.Domain.Entities.Media;
using NamEcommerce.Domain.Services.Media;
using NamEcommerce.Domain.Shared.Events.Media;
using NamEcommerce.Domain.Shared.Exceptions.Media;

namespace NamEcommerce.Domain.Services.Test.Services;

public sealed class PictureManagerTests
{
    #region CreatePictureAsync

    [Fact]
    public async Task CreatePictureAsync_DtoIsNull_ThrowArgumentNullException()
    {
        var pictureManager = new PictureManager(null!, null!);

        await Assert.ThrowsAsync<ArgumentNullException>(() => pictureManager.CreatePictureAsync(null!));
    }

    [Fact]
    public async Task CreatePictureAsync_DataIsInvalid_ThrowsPictureDataIsInvalidException()
    {
        var invalidDto = new CreatePictureDto
        {
            Data = [],
            MimeType = string.Empty
        };
        var pictureManager = new PictureManager(null!, null!);

        await Assert.ThrowsAsync<PictureDataIsInvalidException>(() => pictureManager.CreatePictureAsync(invalidDto));
    }

    [Fact]
    public async Task CreatePictureAsync_DataIsValid_ReturnsCreatedPicture()
    {
        var dto = new CreatePictureDto
        {
            Data = [1, 2, 3],
            MimeType = "image/png",
            FileName = "test.png",
            Extension = ".png"
        };
        var returnPicture = new Picture(dto.Data, dto.MimeType)
        {
            Extension = dto.Extension,
            FileName = dto.FileName
        };
        var pictureRepositoryMock = Repository.Create<Picture>()
            .WhenCall(repo => repo.InsertAsync(It.IsAny<Picture>(), default), returnPicture);
        var pictureManager = new PictureManager(pictureRepositoryMock.Object, null!);

        var result = await pictureManager.CreatePictureAsync(dto);

        Assert.Equal(returnPicture.Id, result.CreatedId);
        pictureRepositoryMock.Verify();
        pictureRepositoryMock.Verify(r => r.InsertAsync(It.Is<Picture>(p =>
            p.DomainEvents.OfType<PictureCreated>().Any(ev => ev.MimeType == dto.MimeType)
            && p.DomainEvents.Count == 1), default), Times.Once);
    }

    #endregion

    #region GetPictureByIdAsync

    [Fact]
    public async Task GetPictureByIdAsync_PictureNotFound_ReturnsNull()
    {
        var notFoundId = Guid.NewGuid();
        var pictureDataReaderMock = EntityDataReader.Create<Picture>().WhenCall(r => r.GetByIdAsync(notFoundId), (Picture?)null);
        var pictureManager = new PictureManager(null!, pictureDataReaderMock.Object);

        var result = await pictureManager.GetPictureByIdAsync(notFoundId);

        Assert.Null(result);
        pictureDataReaderMock.Verify();
    }

    [Fact]
    public async Task GetPictureByIdAsync_PictureFound_ReturnsDto()
    {
        var picture = new Picture(new byte[] { 1 }, "image/png")
        {
            FileName = "a.png",
            Extension = ".png"
        };
        var pictureDataReaderMock = EntityDataReader.Create<Picture>().WhenCall(r => r.GetByIdAsync(picture.Id), picture);
        var pictureManager = new PictureManager(null!, pictureDataReaderMock.Object);

        var result = await pictureManager.GetPictureByIdAsync(picture.Id);

        Assert.NotNull(result);
        Assert.Equal(picture.Id, result!.Id);
        pictureDataReaderMock.Verify();
    }

    #endregion

    #region DeletePictureAsync

    [Fact]
    public async Task DeletePictureAsync_PictureNotFound_ThrowsArgumentException()
    {
        var notFoundId = Guid.NewGuid();
        var pictureDataReaderMock = EntityDataReader.Create<Picture>().WhenCall(r => r.GetByIdAsync(notFoundId), (Picture?)null);
        var pictureManager = new PictureManager(null!, pictureDataReaderMock.Object);

        await Assert.ThrowsAsync<ArgumentException>(() => pictureManager.DeletePictureAsync(notFoundId));

        pictureDataReaderMock.Verify();
    }

    [Fact]
    public async Task DeletePictureAsync_PictureFound_DeletesSuccessfully()
    {
        var picture = new Picture([1], "image/png")
        {
            FileName = "a.png",
            Extension = ".png"
        };
        var pictureRepositoryMock = Repository.Create<Picture>().CanCall(repo => repo.DeleteAsync(It.IsAny<Picture>()));
        var pictureDataReaderMock = EntityDataReader.Create<Picture>().WhenCall(r => r.GetByIdAsync(picture.Id), picture);
        var pictureManager = new PictureManager(pictureRepositoryMock.Object, pictureDataReaderMock.Object);

        await pictureManager.DeletePictureAsync(picture.Id);

        pictureDataReaderMock.Verify();
        pictureRepositoryMock.Verify();
        Assert.Contains(picture.DomainEvents, ev =>
            ev is PictureDeleted deleted && deleted.PictureId == picture.Id);
    }

    #endregion
}
