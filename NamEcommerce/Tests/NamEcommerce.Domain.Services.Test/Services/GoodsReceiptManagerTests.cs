using Moq;
using NamEcommerce.Domain.Entities.GoodsReceipts;
using NamEcommerce.Domain.Entities.Inventory;
using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Domain.Entities.Media;
using NamEcommerce.Domain.Services.GoodsReceipts;
using NamEcommerce.Domain.Services.Test.Helpers;
using NamEcommerce.Domain.Shared.Dtos.GoodsReceipts;
using NamEcommerce.Domain.Shared.Dtos.Users;
using NamEcommerce.Domain.Shared.Enums.Inventory;
using NamEcommerce.Domain.Shared.Events;
using NamEcommerce.Domain.Shared.Events.Entities;
using NamEcommerce.Domain.Entities.Catalog;
using NamEcommerce.Domain.Shared.Exceptions.Catalog;
using NamEcommerce.Domain.Shared.Exceptions.GoodsReceipts;
using NamEcommerce.Domain.Shared.Exceptions.Media;
using NamEcommerce.Domain.Shared.Services.Users;
using NamEcommerce.Domain.Shared.Settings;

namespace NamEcommerce.Domain.Services.Test.Services;

public sealed class GoodsReceiptManagerTests
{
    // ───────────────────────────────────────────────────────────────────────
    // Helpers tạo dữ liệu dùng chung
    // ───────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Tạo một <see cref="GoodsReceipt"/> mới (không có item) qua internal factory.
    /// </summary>
    private static async Task<GoodsReceipt> BuildGoodsReceiptAsync(Guid? id = null)
    {
        var receiptId = id ?? Guid.NewGuid();

        // GetByIdAsync trả về null → không trùng ID
        var noExistingReceiptStub = new Mock<IEntityDataReader<GoodsReceipt>>();
        noExistingReceiptStub
            .Setup(r => r.GetByIdAsync(receiptId))
            .ReturnsAsync((GoodsReceipt?)null);

        var currentUserStub = new Mock<ICurrentUserAccessor>();
        currentUserStub
            .Setup(u => u.GetCurrentUserAsync())
            .ReturnsAsync((CurrentUserInfoDto?)null);

        var goodsReceipt = await GoodsReceipt.CreateAsync(
            receiptId, noExistingReceiptStub.Object, currentUserStub.Object);

        goodsReceipt.SetReceivedDate(DateTime.UtcNow);
        return goodsReceipt;
    }

    /// <summary>
    /// Tạo một <see cref="GoodsReceipt"/> đã có một item.
    /// Dùng WarehouseSettings mặc định (AllowNoWarehouse = false) → phải cung cấp warehouse.
    /// </summary>
    private static async Task<(GoodsReceipt GoodsReceipt, Guid ProductId, Guid WarehouseId)>
        BuildGoodsReceiptWithItemAsync()
    {
        var productId = Guid.NewGuid();
        var warehouseId = Guid.NewGuid();

        var product = new Product(productId, "test-product");
        var warehouse = new Warehouse("wh-code", "test-warehouse", WarehouseType.Main);
        var warehouseSettings = new WarehouseSettings(); // AllowNoWarehouse = false

        var productDataReaderStub = ProductDataReader.ProductById(productId, product);
        var warehouseDataReaderStub = WarehouseDataReader.WarehouseById(warehouseId, warehouse);

        var goodsReceipt = await BuildGoodsReceiptAsync();
        await goodsReceipt.AddItemAsync(
            productId, warehouseId, quantity: 10m, unitCost: null,
            productDataReaderStub.Object, warehouseSettings, warehouseDataReaderStub.Object);

        return (goodsReceipt, productId, warehouseId);
    }

    /// <summary>
    /// Tạo một <see cref="Picture"/> để dùng làm ảnh chứng từ.
    /// Constructor là internal nên truy cập được nhờ InternalsVisibleTo.
    /// </summary>
    private static Picture BuildPicture()
        => new Picture(new byte[1], "image/jpeg");

    // ───────────────────────────────────────────────────────────────────────
    // CreateGoodsReceiptAsync
    // ───────────────────────────────────────────────────────────────────────

    #region CreateGoodsReceiptAsync

    [Fact]
    public async Task CreateGoodsReceiptAsync_DtoIsNull_ThrowsArgumentNullException()
    {
        var manager = new GoodsReceiptManager(null!, null!, null!, null!, null!, null!, null!, null!, null!, null!);

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            manager.CreateGoodsReceiptAsync(null!));
    }

    [Fact]
    public async Task CreateGoodsReceiptAsync_ItemQuantityIsZeroOrNegative_ThrowsGoodsReceiptItemDataIsInvalidException()
    {
        var dto = new CreateGoodsReceiptDto
        {
            ReceivedOnUtc = DateTime.UtcNow,
            PictureIds = [Guid.NewGuid()],
            Items =
            [
                new AddGoodsReceiptItemDto
                {
                    ProductId = Guid.NewGuid(),
                    WarehouseId = null,
                    Quantity = 0m,      // không hợp lệ
                    UnitCost = null
                }
            ]
        };
        var manager = new GoodsReceiptManager(null!, null!, null!, null!, null!, null!, null!, null!, null!, null!);

        await Assert.ThrowsAsync<GoodsReceiptItemDataIsInvalidException>(() =>
            manager.CreateGoodsReceiptAsync(dto));
    }

    [Fact]
    public async Task CreateGoodsReceiptAsync_ItemUnitCostIsNegative_ThrowsGoodsReceiptItemDataIsInvalidException()
    {
        var dto = new CreateGoodsReceiptDto
        {
            ReceivedOnUtc = DateTime.UtcNow,
            PictureIds = [Guid.NewGuid()],
            Items =
            [
                new AddGoodsReceiptItemDto
                {
                    ProductId = Guid.NewGuid(),
                    WarehouseId = null,
                    Quantity = 5m,
                    UnitCost = -1m      // không hợp lệ
                }
            ]
        };
        var manager = new GoodsReceiptManager(null!, null!, null!, null!, null!, null!, null!, null!, null!, null!);

        await Assert.ThrowsAsync<GoodsReceiptItemDataIsInvalidException>(() =>
            manager.CreateGoodsReceiptAsync(dto));
    }

    [Fact]
    public async Task CreateGoodsReceiptAsync_PictureIdsIsEmpty_ThrowsGoodsReceiptProofPictureRequired()
    {
        var dto = new CreateGoodsReceiptDto
        {
            ReceivedOnUtc = DateTime.UtcNow,
            PictureIds = [],            // không có ảnh chứng từ
            Items =
            [
                new AddGoodsReceiptItemDto
                {
                    ProductId = Guid.NewGuid(),
                    WarehouseId = null,
                    Quantity = 5m
                }
            ]
        };
        var manager = new GoodsReceiptManager(null!, null!, null!, null!, null!, null!, null!, null!, null!, null!);

        await Assert.ThrowsAsync<GoodsReceiptProofPictureRequired>(() =>
            manager.CreateGoodsReceiptAsync(dto));
    }

    [Fact]
    public async Task CreateGoodsReceiptAsync_PictureNotFound_ThrowsPictureIsNotFoundException()
    {
        var productId = Guid.NewGuid();
        var warehouseId = Guid.NewGuid();
        var notFoundPictureId = Guid.NewGuid();

        var product = new Product(productId, "product");
        var warehouse = new Warehouse("wh", "warehouse", WarehouseType.Main);
        var warehouseSettings = new WarehouseSettings();

        // GoodsReceipt.CreateAsync sinh ID mới → dùng mock trả null cho mọi GetByIdAsync
        var goodsReceiptDataReaderStub = new Mock<IEntityDataReader<GoodsReceipt>>();
        goodsReceiptDataReaderStub
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync((GoodsReceipt?)null);

        var currentUserStub = new Mock<ICurrentUserAccessor>();
        currentUserStub.Setup(u => u.GetCurrentUserAsync()).ReturnsAsync((CurrentUserInfoDto?)null);

        var productDataReaderStub = ProductDataReader.ProductById(productId, product);
        var warehouseDataReaderStub = WarehouseDataReader.WarehouseById(warehouseId, warehouse);
        var pictureDataReaderMock = PictureDataReader.NotFound(notFoundPictureId);

        var dto = new CreateGoodsReceiptDto
        {
            ReceivedOnUtc = DateTime.UtcNow,
            TruckDriverName = "Nguyễn Văn A",
            PictureIds = [notFoundPictureId],
            Items =
            [
                new AddGoodsReceiptItemDto
                {
                    ProductId = productId,
                    WarehouseId = warehouseId,
                    Quantity = 5m
                }
            ]
        };

        var manager = new GoodsReceiptManager(
            null!, goodsReceiptDataReaderStub.Object,
            productDataReaderStub.Object, warehouseSettings,
            warehouseDataReaderStub.Object, currentUserStub.Object,
            pictureDataReaderMock.Object, null!, null!, null!);

        await Assert.ThrowsAsync<PictureIsNotFoundException>(() =>
            manager.CreateGoodsReceiptAsync(dto));
    }

    [Fact]
    public async Task CreateGoodsReceiptAsync_ValidDto_CreatesAndReturnsCreatedId()
    {
        var productId = Guid.NewGuid();
        var warehouseId = Guid.NewGuid();
        var pictureId = Guid.NewGuid();

        var product = new Product(productId, "product");
        var warehouse = new Warehouse("wh", "warehouse", WarehouseType.Main);
        var picture = BuildPicture();
        var warehouseSettings = new WarehouseSettings();

        var goodsReceiptDataReaderStub = new Mock<IEntityDataReader<GoodsReceipt>>();
        goodsReceiptDataReaderStub
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync((GoodsReceipt?)null);

        var currentUserStub = new Mock<ICurrentUserAccessor>();
        currentUserStub.Setup(u => u.GetCurrentUserAsync()).ReturnsAsync((CurrentUserInfoDto?)null);

        var productDataReaderStub = ProductDataReader.ProductById(productId, product);
        var warehouseDataReaderStub = WarehouseDataReader.WarehouseById(warehouseId, warehouse);
        var pictureDataReaderStub = PictureDataReader.PictureById(pictureId, picture);

        var goodsReceiptRepositoryMock = new Mock<IRepository<GoodsReceipt>>();
        goodsReceiptRepositoryMock
            .Setup(r => r.InsertAsync(It.IsAny<GoodsReceipt>(), default))
            .ReturnsAsync((GoodsReceipt gr, CancellationToken _) => gr)
            .Verifiable();

        var dto = new CreateGoodsReceiptDto
        {
            ReceivedOnUtc = DateTime.UtcNow,
            TruckDriverName = "Nguyễn Văn A",
            TruckNumberSerial = "51K-123.45",
            PictureIds = [pictureId],
            Items =
            [
                new AddGoodsReceiptItemDto
                {
                    ProductId = productId,
                    WarehouseId = warehouseId,
                    Quantity = 10m
                }
            ]
        };

        var manager = new GoodsReceiptManager(
            goodsReceiptRepositoryMock.Object, goodsReceiptDataReaderStub.Object,
            productDataReaderStub.Object, warehouseSettings,
            warehouseDataReaderStub.Object, currentUserStub.Object,
            pictureDataReaderStub.Object, null!, null!, Mock.Of<IEventPublisher>());

        var result = await manager.CreateGoodsReceiptAsync(dto);

        Assert.NotEqual(Guid.Empty, result.CreatedId);
        goodsReceiptRepositoryMock.Verify();
    }

    #endregion

    // ───────────────────────────────────────────────────────────────────────
    // UpdateGoodsReceiptAsync
    // ───────────────────────────────────────────────────────────────────────

    #region UpdateGoodsReceiptAsync

    [Fact]
    public async Task UpdateGoodsReceiptAsync_DtoIsNull_ThrowsArgumentNullException()
    {
        var manager = new GoodsReceiptManager(null!, null!, null!, null!, null!, null!, null!, null!, null!, null!);

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            manager.UpdateGoodsReceiptAsync(null!));
    }

    [Fact]
    public async Task UpdateGoodsReceiptAsync_PictureIdsIsEmpty_ThrowsGoodsReceiptProofPictureRequired()
    {
        var dto = new UpdateGoodsReceiptDto(Guid.NewGuid())
        {
            ReceivedOnUtc = DateTime.UtcNow,
            PictureIds = []             // không có ảnh chứng từ
        };
        var manager = new GoodsReceiptManager(null!, null!, null!, null!, null!, null!, null!, null!, null!, null!);

        await Assert.ThrowsAsync<GoodsReceiptProofPictureRequired>(() =>
            manager.UpdateGoodsReceiptAsync(dto));
    }

    [Fact]
    public async Task UpdateGoodsReceiptAsync_GoodsReceiptNotFound_ThrowsGoodsReceiptIsNotFoundException()
    {
        var notFoundId = Guid.NewGuid();
        var dto = new UpdateGoodsReceiptDto(notFoundId)
        {
            ReceivedOnUtc = DateTime.UtcNow,
            PictureIds = [Guid.NewGuid()]
        };
        var goodsReceiptDataReaderMock = GoodsReceiptDataReader.NotFound(notFoundId);
        var manager = new GoodsReceiptManager(
            null!, goodsReceiptDataReaderMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        await Assert.ThrowsAsync<GoodsReceiptIsNotFoundException>(() =>
            manager.UpdateGoodsReceiptAsync(dto));

        goodsReceiptDataReaderMock.Verify();
    }

    [Fact]
    public async Task UpdateGoodsReceiptAsync_PictureNotFound_ThrowsPictureIsNotFoundException()
    {
        var goodsReceipt = await BuildGoodsReceiptAsync();
        var notFoundPictureId = Guid.NewGuid();

        var dto = new UpdateGoodsReceiptDto(goodsReceipt.Id)
        {
            ReceivedOnUtc = DateTime.UtcNow,
            TruckDriverName = "Trần B",
            PictureIds = [notFoundPictureId]
        };

        var goodsReceiptDataReaderStub = GoodsReceiptDataReader.GoodsReceiptById(goodsReceipt.Id, goodsReceipt);
        var pictureDataReaderMock = PictureDataReader.NotFound(notFoundPictureId);

        var manager = new GoodsReceiptManager(
            null!, goodsReceiptDataReaderStub.Object,
            null!, null!, null!, null!,
            pictureDataReaderMock.Object, null!, null!, null!);

        await Assert.ThrowsAsync<PictureIsNotFoundException>(() =>
            manager.UpdateGoodsReceiptAsync(dto));
    }

    [Fact]
    public async Task UpdateGoodsReceiptAsync_ValidDto_UpdatesAndReturnsUpdatedId()
    {
        var pictureId = Guid.NewGuid();
        var picture = BuildPicture();
        var goodsReceipt = await BuildGoodsReceiptAsync();

        var dto = new UpdateGoodsReceiptDto(goodsReceipt.Id)
        {
            ReceivedOnUtc = DateTime.UtcNow,
            TruckDriverName = "Lê Văn C",
            TruckNumberSerial = "60K-999.88",
            PictureIds = [pictureId]
        };

        var goodsReceiptDataReaderStub = GoodsReceiptDataReader.GoodsReceiptById(goodsReceipt.Id, goodsReceipt);
        var pictureDataReaderStub = PictureDataReader.PictureById(pictureId, picture);

        var goodsReceiptRepositoryMock = new Mock<IRepository<GoodsReceipt>>();
        goodsReceiptRepositoryMock
            .Setup(r => r.UpdateAsync(It.Is<GoodsReceipt>(gr => gr.Id == goodsReceipt.Id), default))
            .ReturnsAsync(goodsReceipt)
            .Verifiable();

        var manager = new GoodsReceiptManager(
            goodsReceiptRepositoryMock.Object, goodsReceiptDataReaderStub.Object,
            null!, null!, null!, null!,
            pictureDataReaderStub.Object, null!, null!, Mock.Of<IEventPublisher>());

        var result = await manager.UpdateGoodsReceiptAsync(dto);

        Assert.Equal(goodsReceipt.Id, result.UpdatedId);
        goodsReceiptRepositoryMock.Verify();
    }

    #endregion

    // ───────────────────────────────────────────────────────────────────────
    // DeleteGoodsReceiptAsync
    // ───────────────────────────────────────────────────────────────────────

    #region DeleteGoodsReceiptAsync

    [Fact]
    public async Task DeleteGoodsReceiptAsync_GoodsReceiptNotFound_ThrowsGoodsReceiptIsNotFoundException()
    {
        var notFoundId = Guid.NewGuid();
        var dto = new DeleteGoodsReceiptDto(notFoundId)
        {
            ReceivedOnUtc = DateTime.UtcNow,
            PictureIds = []
        };
        var goodsReceiptDataReaderMock = GoodsReceiptDataReader.NotFound(notFoundId);
        var manager = new GoodsReceiptManager(
            null!, goodsReceiptDataReaderMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        await Assert.ThrowsAsync<GoodsReceiptIsNotFoundException>(() =>
            manager.DeleteGoodsReceiptAsync(dto));

        goodsReceiptDataReaderMock.Verify();
    }

    [Fact]
    public async Task DeleteGoodsReceiptAsync_ValidDto_DeletesGoodsReceiptAndPublishesEvent()
    {
        var goodsReceipt = await BuildGoodsReceiptAsync();
        var dto = new DeleteGoodsReceiptDto(goodsReceipt.Id)
        {
            ReceivedOnUtc = DateTime.UtcNow,
            PictureIds = []
        };

        var goodsReceiptDataReaderStub = GoodsReceiptDataReader.GoodsReceiptById(goodsReceipt.Id, goodsReceipt);
        var goodsReceiptRepositoryMock = GoodsReceiptRepository.CanDelete(goodsReceipt);

        var manager = new GoodsReceiptManager(
            goodsReceiptRepositoryMock.Object, goodsReceiptDataReaderStub.Object,
            null!, null!, null!, null!, null!, null!, null!, Mock.Of<IEventPublisher>());

        await manager.DeleteGoodsReceiptAsync(dto);

        goodsReceiptRepositoryMock.Verify();
    }

    #endregion

    // ───────────────────────────────────────────────────────────────────────
    // SetGoodsReceiptItemUnitCostAsync
    // ───────────────────────────────────────────────────────────────────────

    #region SetGoodsReceiptItemUnitCostAsync

    [Fact]
    public async Task SetGoodsReceiptItemUnitCostAsync_DtoIsNull_ThrowsArgumentNullException()
    {
        var manager = new GoodsReceiptManager(null!, null!, null!, null!, null!, null!, null!, null!, null!, null!);

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            manager.SetGoodsReceiptItemUnitCostAsync(null!));
    }

    [Fact]
    public async Task SetGoodsReceiptItemUnitCostAsync_UnitCostIsNegative_ThrowsGoodsReceiptItemDataIsInvalidException()
    {
        var dto = new SetGoodsReceiptItemUnitCostDto
        {
            GoodsReceiptId = Guid.NewGuid(),
            GoodsReceiptItemId = Guid.NewGuid(),
            UnitCost = -1m              // không hợp lệ
        };
        var manager = new GoodsReceiptManager(null!, null!, null!, null!, null!, null!, null!, null!, null!, null!);

        await Assert.ThrowsAsync<GoodsReceiptItemDataIsInvalidException>(() =>
            manager.SetGoodsReceiptItemUnitCostAsync(dto));
    }

    [Fact]
    public async Task SetGoodsReceiptItemUnitCostAsync_GoodsReceiptNotFound_ThrowsGoodsReceiptIsNotFoundException()
    {
        var notFoundId = Guid.NewGuid();
        var dto = new SetGoodsReceiptItemUnitCostDto
        {
            GoodsReceiptId = notFoundId,
            GoodsReceiptItemId = Guid.NewGuid(),
            UnitCost = 100_000m
        };
        var goodsReceiptDataReaderMock = GoodsReceiptDataReader.NotFound(notFoundId);
        var manager = new GoodsReceiptManager(
            null!, goodsReceiptDataReaderMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        await Assert.ThrowsAsync<GoodsReceiptIsNotFoundException>(() =>
            manager.SetGoodsReceiptItemUnitCostAsync(dto));

        goodsReceiptDataReaderMock.Verify();
    }

    [Fact]
    public async Task SetGoodsReceiptItemUnitCostAsync_ItemNotFound_ThrowsGoodsReceiptItemIsNotFoundException()
    {
        // GoodsReceipt không có item → bất kỳ itemId nào cũng không tìm thấy
        var goodsReceipt = await BuildGoodsReceiptAsync();
        var dto = new SetGoodsReceiptItemUnitCostDto
        {
            GoodsReceiptId = goodsReceipt.Id,
            GoodsReceiptItemId = Guid.NewGuid(),    // không tồn tại
            UnitCost = 100_000m
        };
        var goodsReceiptDataReaderStub = GoodsReceiptDataReader.GoodsReceiptById(goodsReceipt.Id, goodsReceipt);
        var manager = new GoodsReceiptManager(
            null!, goodsReceiptDataReaderStub.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        await Assert.ThrowsAsync<GoodsReceiptItemIsNotFoundException>(() =>
            manager.SetGoodsReceiptItemUnitCostAsync(dto));
    }

    [Fact]
    public async Task SetGoodsReceiptItemUnitCostAsync_ValidDto_SetsUnitCostAndUpdates()
    {
        var (goodsReceipt, _, _) = await BuildGoodsReceiptWithItemAsync();

        // GoodsReceiptItem.Id = Guid.Empty (base class nhận Guid.Empty trong constructor)
        var itemId = goodsReceipt.Items.First().Id;
        var dto = new SetGoodsReceiptItemUnitCostDto
        {
            GoodsReceiptId = goodsReceipt.Id,
            GoodsReceiptItemId = itemId,
            UnitCost = 200_000m
        };

        var goodsReceiptDataReaderStub = GoodsReceiptDataReader.GoodsReceiptById(goodsReceipt.Id, goodsReceipt);

        var goodsReceiptRepositoryMock = new Mock<IRepository<GoodsReceipt>>();
        goodsReceiptRepositoryMock
            .Setup(r => r.UpdateAsync(It.Is<GoodsReceipt>(gr => gr.Id == goodsReceipt.Id), default))
            .ReturnsAsync(goodsReceipt)
            .Verifiable();

        var manager = new GoodsReceiptManager(
            goodsReceiptRepositoryMock.Object, goodsReceiptDataReaderStub.Object,
            null!, null!, null!, null!, null!, null!, null!, Mock.Of<IEventPublisher>());

        await manager.SetGoodsReceiptItemUnitCostAsync(dto);

        Assert.Equal(200_000m, goodsReceipt.Items.First().UnitCost);
        goodsReceiptRepositoryMock.Verify();
    }

    [Fact]
    public async Task SetGoodsReceiptItemUnitCostAsync_ValidDto_PublishesEntityUpdatedWithItemIdAsAdditionalData()
    {
        // Mục đích test: GoodsReceiptUpdatedHandler cần phân biệt được "đây là SetUnitCost"
        // thông qua AdditionalData == item.Id để chạy Full Recalculation AverageCost.
        var (goodsReceipt, _, _) = await BuildGoodsReceiptWithItemAsync();

        var itemId = goodsReceipt.Items.First().Id;
        var dto = new SetGoodsReceiptItemUnitCostDto
        {
            GoodsReceiptId = goodsReceipt.Id,
            GoodsReceiptItemId = itemId,
            UnitCost = 200_000m
        };

        var goodsReceiptDataReaderStub = GoodsReceiptDataReader.GoodsReceiptById(goodsReceipt.Id, goodsReceipt);

        var goodsReceiptRepositoryStub = new Mock<IRepository<GoodsReceipt>>();
        goodsReceiptRepositoryStub
            .Setup(r => r.UpdateAsync(It.IsAny<GoodsReceipt>(), default))
            .ReturnsAsync(goodsReceipt);

        // Verify rằng PublishEvent được gọi với EntityUpdatedEvent có AdditionalData == itemId.
        // Dùng PublishEvent (low-level) thay vì extension method EntityUpdated() vì extension không mock được.
        var eventPublisherMock = new Mock<IEventPublisher>();
        eventPublisherMock
            .Setup(p => p.PublishEvent<EntityUpdatedEvent<GoodsReceipt>, GoodsReceipt>(
                It.Is<EntityUpdatedEvent<GoodsReceipt>>(e =>
                    e.Entity.Id == goodsReceipt.Id && (Guid)e.AdditionalData! == itemId)))
            .Returns(Task.CompletedTask)
            .Verifiable();

        var manager = new GoodsReceiptManager(
            goodsReceiptRepositoryStub.Object, goodsReceiptDataReaderStub.Object,
            null!, null!, null!, null!, null!, null!, null!, eventPublisherMock.Object);

        await manager.SetGoodsReceiptItemUnitCostAsync(dto);

        eventPublisherMock.Verify();
    }

    #endregion

    // ───────────────────────────────────────────────────────────────────────
    // GetGoodsReceiptByIdAsync
    // ───────────────────────────────────────────────────────────────────────

    #region GetGoodsReceiptByIdAsync

    [Fact]
    public async Task GetGoodsReceiptByIdAsync_GoodsReceiptNotFound_ReturnsNull()
    {
        var notFoundId = Guid.NewGuid();
        var goodsReceiptDataReaderMock = GoodsReceiptDataReader.NotFound(notFoundId);
        var manager = new GoodsReceiptManager(
            null!, goodsReceiptDataReaderMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        var result = await manager.GetGoodsReceiptByIdAsync(notFoundId);

        Assert.Null(result);
        goodsReceiptDataReaderMock.Verify();
    }

    [Fact]
    public async Task GetGoodsReceiptByIdAsync_GoodsReceiptFound_ReturnsDtoWithCorrectId()
    {
        var goodsReceipt = await BuildGoodsReceiptAsync();
        var goodsReceiptDataReaderMock = GoodsReceiptDataReader.GoodsReceiptById(goodsReceipt.Id, goodsReceipt);
        var manager = new GoodsReceiptManager(
            null!, goodsReceiptDataReaderMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        var result = await manager.GetGoodsReceiptByIdAsync(goodsReceipt.Id);

        Assert.NotNull(result);
        Assert.Equal(goodsReceipt.Id, result.Id);
        goodsReceiptDataReaderMock.Verify();
    }

    #endregion

    // ───────────────────────────────────────────────────────────────────────
    // GetGoodsReceiptsAsync
    // ───────────────────────────────────────────────────────────────────────

    #region GetGoodsReceiptsAsync

    [Fact]
    public async Task GetGoodsReceiptsAsync_PageIndexLessThan0_ThrowsArgumentOutOfRangeException()
    {
        var manager = new GoodsReceiptManager(null!, null!, null!, null!, null!, null!, null!, null!, null!, null!);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            manager.GetGoodsReceiptsAsync(pageIndex: -1, pageSize: 10,
                keywords: null, fromDateUtc: null, toDateUtc: null));
    }

    [Fact]
    public async Task GetGoodsReceiptsAsync_PageSizeLessThanOrEqualTo0_ThrowsArgumentOutOfRangeException()
    {
        var manager = new GoodsReceiptManager(null!, null!, null!, null!, null!, null!, null!, null!, null!, null!);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            manager.GetGoodsReceiptsAsync(pageIndex: 0, pageSize: 0,
                keywords: null, fromDateUtc: null, toDateUtc: null));
    }

    [Fact]
    public async Task GetGoodsReceiptsAsync_NoKeywords_ReturnsAllOrderedByCreatedOnUtcDescending()
    {
        // Tạo 3 phiếu; phiếu tạo cuối cùng phải đứng đầu kết quả (desc)
        var gr1 = await BuildGoodsReceiptAsync();
        gr1.SetReceivedDate(new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        var gr2 = await BuildGoodsReceiptAsync();
        gr2.SetReceivedDate(new DateTime(2024, 6, 1, 0, 0, 0, DateTimeKind.Utc));
        var gr3 = await BuildGoodsReceiptAsync();
        gr3.SetReceivedDate(new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)); // mới nhất

        var goodsReceiptDataReaderStub = GoodsReceiptDataReader.WithData(gr1, gr2, gr3);
        var manager = new GoodsReceiptManager(
            null!, goodsReceiptDataReaderStub.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        var result = await manager.GetGoodsReceiptsAsync(
            pageIndex: 0, pageSize: 1, keywords: null, fromDateUtc: null, toDateUtc: null);

        Assert.Equal(3, result.PagerInfo.TotalCount);
        Assert.Equal(gr3.Id, result.First().Id);        // mới nhất đứng đầu
        goodsReceiptDataReaderStub.Verify();
    }

    [Fact]
    public async Task GetGoodsReceiptsAsync_KeywordsMatchesTruckDriverName_ReturnsFilteredResults()
    {
        var matchedGr = await BuildGoodsReceiptAsync();
        matchedGr.TruckDriverName = "Nguyen Tai Xe";   // không dấu để tránh phụ thuộc normalize
        var unmatchedGr = await BuildGoodsReceiptAsync();
        unmatchedGr.TruckDriverName = "Khac Hoan Toan";

        var goodsReceiptDataReaderStub = GoodsReceiptDataReader.WithData(matchedGr, unmatchedGr);
        var manager = new GoodsReceiptManager(
            null!, goodsReceiptDataReaderStub.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        var result = await manager.GetGoodsReceiptsAsync(
            pageIndex: 0, pageSize: 10, keywords: "Tai Xe", fromDateUtc: null, toDateUtc: null);

        Assert.Equal(1, result.PagerInfo.TotalCount);
        Assert.Equal(matchedGr.Id, result.First().Id);
        goodsReceiptDataReaderStub.Verify();
    }

    [Fact]
    public async Task GetGoodsReceiptsAsync_KeywordsMatchesTruckNumberSerial_ReturnsFilteredResults()
    {
        var matchedGr = await BuildGoodsReceiptAsync();
        matchedGr.TruckNumberSerial = "51K-123.45";
        var unmatchedGr = await BuildGoodsReceiptAsync();
        unmatchedGr.TruckNumberSerial = "60H-999.00";

        var goodsReceiptDataReaderStub = GoodsReceiptDataReader.WithData(matchedGr, unmatchedGr);
        var manager = new GoodsReceiptManager(
            null!, goodsReceiptDataReaderStub.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        var result = await manager.GetGoodsReceiptsAsync(
            pageIndex: 0, pageSize: 10, keywords: "51K", fromDateUtc: null, toDateUtc: null);

        Assert.Equal(1, result.PagerInfo.TotalCount);
        Assert.Equal(matchedGr.Id, result.First().Id);
        goodsReceiptDataReaderStub.Verify();
    }

    [Fact]
    public async Task GetGoodsReceiptsAsync_FromDateFilter_ReturnsOnlyReceiptsOnOrAfterFromDate()
    {
        var oldGr = await BuildGoodsReceiptAsync();
        oldGr.SetReceivedDate(new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        var newGr = await BuildGoodsReceiptAsync();
        newGr.SetReceivedDate(new DateTime(2025, 6, 1, 0, 0, 0, DateTimeKind.Utc));

        var goodsReceiptDataReaderStub = GoodsReceiptDataReader.WithData(oldGr, newGr);
        var manager = new GoodsReceiptManager(
            null!, goodsReceiptDataReaderStub.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        var fromDate = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var result = await manager.GetGoodsReceiptsAsync(
            pageIndex: 0, pageSize: 10, keywords: null, fromDateUtc: fromDate, toDateUtc: null);

        Assert.Equal(1, result.PagerInfo.TotalCount);
        Assert.Equal(newGr.Id, result.First().Id);
        goodsReceiptDataReaderStub.Verify();
    }

    [Fact]
    public async Task GetGoodsReceiptsAsync_ToDateFilter_ReturnsOnlyReceiptsOnOrBeforeToDate()
    {
        var oldGr = await BuildGoodsReceiptAsync();
        oldGr.SetReceivedDate(new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        var newGr = await BuildGoodsReceiptAsync();
        newGr.SetReceivedDate(new DateTime(2025, 6, 1, 0, 0, 0, DateTimeKind.Utc));

        var goodsReceiptDataReaderStub = GoodsReceiptDataReader.WithData(oldGr, newGr);
        var manager = new GoodsReceiptManager(
            null!, goodsReceiptDataReaderStub.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        var toDate = new DateTime(2024, 12, 31, 23, 59, 59, DateTimeKind.Utc);
        var result = await manager.GetGoodsReceiptsAsync(
            pageIndex: 0, pageSize: 10, keywords: null, fromDateUtc: null, toDateUtc: toDate);

        Assert.Equal(1, result.PagerInfo.TotalCount);
        Assert.Equal(oldGr.Id, result.First().Id);
        goodsReceiptDataReaderStub.Verify();
    }

    [Fact]
    public async Task GetGoodsReceiptsAsync_DateRangeFilter_ReturnsOnlyReceiptsWithinRange()
    {
        var earlyGr = await BuildGoodsReceiptAsync();
        earlyGr.SetReceivedDate(new DateTime(2023, 6, 1, 0, 0, 0, DateTimeKind.Utc));
        var midGr = await BuildGoodsReceiptAsync();
        midGr.SetReceivedDate(new DateTime(2024, 6, 1, 0, 0, 0, DateTimeKind.Utc));
        var lateGr = await BuildGoodsReceiptAsync();
        lateGr.SetReceivedDate(new DateTime(2025, 6, 1, 0, 0, 0, DateTimeKind.Utc));

        var goodsReceiptDataReaderStub = GoodsReceiptDataReader.WithData(earlyGr, midGr, lateGr);
        var manager = new GoodsReceiptManager(
            null!, goodsReceiptDataReaderStub.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        var fromDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var toDate = new DateTime(2024, 12, 31, 23, 59, 59, DateTimeKind.Utc);
        var result = await manager.GetGoodsReceiptsAsync(
            pageIndex: 0, pageSize: 10, keywords: null, fromDateUtc: fromDate, toDateUtc: toDate);

        Assert.Equal(1, result.PagerInfo.TotalCount);
        Assert.Equal(midGr.Id, result.First().Id);
        goodsReceiptDataReaderStub.Verify();
    }

    [Fact]
    public async Task GetGoodsReceiptsAsync_PaginationApplied_ReturnsCorrectPageAndTotalCount()
    {
        var receipts = new List<GoodsReceipt>();
        for (var i = 0; i < 5; i++)
        {
            var gr = await BuildGoodsReceiptAsync();
            gr.SetReceivedDate(new DateTime(2024, 1, i + 1, 0, 0, 0, DateTimeKind.Utc));
            receipts.Add(gr);
        }

        var goodsReceiptDataReaderStub = GoodsReceiptDataReader.WithData(receipts.ToArray());
        var manager = new GoodsReceiptManager(
            null!, goodsReceiptDataReaderStub.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        var result = await manager.GetGoodsReceiptsAsync(
            pageIndex: 1, pageSize: 2, keywords: null, fromDateUtc: null, toDateUtc: null);

        Assert.Equal(5, result.PagerInfo.TotalCount);   // tổng vẫn là 5
        Assert.Equal(2, result.Count());                // trang 2 có 2 items
        goodsReceiptDataReaderStub.Verify();
    }

    #endregion

    // ───────────────────────────────────────────────────────────────────────
    // SetGoodsReceiptVendorAsync
    // ───────────────────────────────────────────────────────────────────────

    #region SetGoodsReceiptVendorAsync

    [Fact]
    public async Task SetGoodsReceiptVendorAsync_DtoIsNull_ThrowsArgumentNullException()
    {
        var manager = new GoodsReceiptManager(null!, null!, null!, null!, null!, null!, null!, null!, null!, null!);

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            manager.SetGoodsReceiptVendorAsync(null!));
    }

    [Fact]
    public async Task SetGoodsReceiptVendorAsync_GoodsReceiptIdIsEmpty_ThrowsGoodsReceiptIsNotFoundException()
    {
        var dto = new SetGoodsReceiptVendorDto(Guid.Empty)
        {
            VendorId = Guid.NewGuid()
        };
        var manager = new GoodsReceiptManager(null!, null!, null!, null!, null!, null!, null!, null!, null!, null!);

        await Assert.ThrowsAsync<GoodsReceiptIsNotFoundException>(() =>
            manager.SetGoodsReceiptVendorAsync(dto));
    }

    [Fact]
    public async Task SetGoodsReceiptVendorAsync_GoodsReceiptNotFound_ThrowsGoodsReceiptIsNotFoundException()
    {
        var notFoundId = Guid.NewGuid();
        var dto = new SetGoodsReceiptVendorDto(notFoundId)
        {
            VendorId = Guid.NewGuid()
        };
        var goodsReceiptDataReaderMock = GoodsReceiptDataReader.NotFound(notFoundId);
        var manager = new GoodsReceiptManager(
            null!, goodsReceiptDataReaderMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        await Assert.ThrowsAsync<GoodsReceiptIsNotFoundException>(() =>
            manager.SetGoodsReceiptVendorAsync(dto));
    }

    [Fact]
    public async Task SetGoodsReceiptVendorAsync_VendorNotFound_ThrowsVendorIsNotFoundException()
    {
        var goodsReceipt = await BuildGoodsReceiptAsync();
        var notFoundVendorId = Guid.NewGuid();

        var dto = new SetGoodsReceiptVendorDto(goodsReceipt.Id)
        {
            VendorId = notFoundVendorId
        };

        var goodsReceiptDataReaderStub = GoodsReceiptDataReader.GoodsReceiptById(goodsReceipt.Id, goodsReceipt);
        var vendorDataReaderMock = VendorDataReader.NotFound(notFoundVendorId);

        var manager = new GoodsReceiptManager(
            null!, goodsReceiptDataReaderStub.Object,
            null!, null!, null!, null!, null!, null!,
            vendorDataReaderMock.Object, null!);

        await Assert.ThrowsAsync<VendorIsNotFoundException>(() =>
            manager.SetGoodsReceiptVendorAsync(dto));
    }

    [Fact]
    public async Task SetGoodsReceiptVendorAsync_ValidVendorId_SetsVendorAndPublishesEvent()
    {
        var goodsReceipt = await BuildGoodsReceiptAsync();
        var vendorId = Guid.NewGuid();
        var vendor = new Vendor(vendorId, "Nhà cung cấp ABC", "0901234567");

        var dto = new SetGoodsReceiptVendorDto(goodsReceipt.Id)
        {
            VendorId = vendorId
        };

        var goodsReceiptDataReaderStub = GoodsReceiptDataReader.GoodsReceiptById(goodsReceipt.Id, goodsReceipt);
        var vendorDataReaderStub = VendorDataReader.VendorById(vendorId, vendor);

        var goodsReceiptRepositoryMock = new Mock<IRepository<GoodsReceipt>>();
        goodsReceiptRepositoryMock
            .Setup(r => r.UpdateAsync(It.Is<GoodsReceipt>(gr =>
                gr.Id == goodsReceipt.Id
                && gr.VendorId == vendorId
                && gr.VendorName == vendor.Name), default))
            .ReturnsAsync(goodsReceipt)
            .Verifiable();

        var manager = new GoodsReceiptManager(
            goodsReceiptRepositoryMock.Object, goodsReceiptDataReaderStub.Object,
            null!, null!, null!, null!, null!, null!,
            vendorDataReaderStub.Object, null!);

        var result = await manager.SetGoodsReceiptVendorAsync(dto);

        Assert.Equal(goodsReceipt.Id, result.UpdatedId);
        goodsReceiptRepositoryMock.Verify();
    }

    [Fact]
    public async Task SetGoodsReceiptVendorAsync_NullVendorId_ClearsVendorAndPublishesEvent()
    {
        var goodsReceipt = await BuildGoodsReceiptAsync();
        var existingVendorId = Guid.NewGuid();
        // Set vendor trước
        goodsReceipt.SetVendor(existingVendorId, "Old Vendor", "0900000000", null);

        var dto = new SetGoodsReceiptVendorDto(goodsReceipt.Id)
        {
            VendorId = null    // null = xoá vendor
        };

        var goodsReceiptDataReaderStub = GoodsReceiptDataReader.GoodsReceiptById(goodsReceipt.Id, goodsReceipt);

        var goodsReceiptRepositoryMock = new Mock<IRepository<GoodsReceipt>>();
        goodsReceiptRepositoryMock
            .Setup(r => r.UpdateAsync(It.Is<GoodsReceipt>(gr =>
                gr.Id == goodsReceipt.Id
                && gr.VendorId == null), default))
            .ReturnsAsync(goodsReceipt)
            .Verifiable();

        var manager = new GoodsReceiptManager(
            goodsReceiptRepositoryMock.Object, goodsReceiptDataReaderStub.Object,
            null!, null!, null!, null!, null!, null!, null!, null!);

        var result = await manager.SetGoodsReceiptVendorAsync(dto);

        Assert.Equal(goodsReceipt.Id, result.UpdatedId);
        goodsReceiptRepositoryMock.Verify();
    }

    #endregion
}
