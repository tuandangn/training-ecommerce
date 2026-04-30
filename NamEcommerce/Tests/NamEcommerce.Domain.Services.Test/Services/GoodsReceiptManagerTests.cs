using NamEcommerce.Domain.Entities.GoodsReceipts;
using NamEcommerce.Domain.Entities.Inventory;
using NamEcommerce.Domain.Entities.PurchaseOrders;
using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Domain.Entities.Media;
using NamEcommerce.Domain.Services.GoodsReceipts;
using NamEcommerce.Domain.Services.Test.Helpers;
using NamEcommerce.Domain.Shared.Dtos.GoodsReceipts;
using NamEcommerce.Domain.Shared.Dtos.Users;
using NamEcommerce.Domain.Shared.Enums.Inventory;
using NamEcommerce.Domain.Shared.Enums.PurchaseOrders;
using NamEcommerce.Domain.Shared.Events;
using NamEcommerce.Domain.Shared.Events.Entities;
using NamEcommerce.Domain.Shared.Exceptions.GoodsReceipts;
using NamEcommerce.Domain.Shared.Exceptions.Media;
using NamEcommerce.Domain.Shared.Services.Users;
using NamEcommerce.Domain.Shared.Settings;
using System.Reflection;

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
        var manager = new GoodsReceiptManager(null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!);

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
        var manager = new GoodsReceiptManager(null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!);

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
        var manager = new GoodsReceiptManager(null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!);

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
        var manager = new GoodsReceiptManager(null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!);

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
            pictureDataReaderMock.Object, null!, null!, null!, null!);

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
            pictureDataReaderStub.Object, null!, null!, null!, null!);

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
        var manager = new GoodsReceiptManager(null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!);

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
        var manager = new GoodsReceiptManager(null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!);

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
            null!, goodsReceiptDataReaderMock.Object, null!, null!, null!, null!, null!, null!, null!, null!, null!);

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
            pictureDataReaderMock.Object, null!, null!, null!, null!);

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
            pictureDataReaderStub.Object, null!, null!, null!, null!);

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
            null!, goodsReceiptDataReaderMock.Object, null!, null!, null!, null!, null!, null!, null!, null!, null!);

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
            null!, null!, null!, null!, null!, null!, null!, null!, null!);

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
        var manager = new GoodsReceiptManager(null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!);

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
        var manager = new GoodsReceiptManager(null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!);

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
            null!, goodsReceiptDataReaderMock.Object, null!, null!, null!, null!, null!, null!, null!, null!, null!);

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
            null!, goodsReceiptDataReaderStub.Object, null!, null!, null!, null!, null!, null!, null!, null!, null!);

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
            null!, null!, null!, null!, null!, null!, null!, null!, null!);

        await manager.SetGoodsReceiptItemUnitCostAsync(dto);

        Assert.Equal(200_000m, goodsReceipt.Items.First().UnitCost);
        goodsReceiptRepositoryMock.Verify();
    }

    // [DEPRECATED — session 2026-04-30] Test cũ verify GoodsReceiptManager.SetGoodsReceiptItemUnitCostAsync
    // publish `EntityUpdatedEvent<GoodsReceipt>` với `AdditionalData == itemId`. Sau migration sang
    // Domain Event mới (session 2026-04-30), Manager raise concrete event `GoodsReceiptItemUnitCostSet(Id, itemId)`
    // qua `entity.MarkItemUnitCostSet(...)` và dispatch qua `DomainEventDispatchInterceptor` sau SaveChanges.
    //
    // Test thay thế (Tuấn tự bổ sung sau theo CLAUDE.md "Tạm thời KHÔNG viết unit test mới"):
    //   Assert.Contains(goodsReceipt.DomainEvents,
    //       e => e is GoodsReceiptItemUnitCostSet { GoodsReceiptItemId: var id } && id == itemId);
    //
    // [Fact]
    // public async Task SetGoodsReceiptItemUnitCostAsync_ValidDto_RaisesItemUnitCostSetEvent()
    // {
    //     ...
    // }

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
            null!, goodsReceiptDataReaderMock.Object, null!, null!, null!, null!, null!, null!, null!, null!, null!);

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
            null!, goodsReceiptDataReaderMock.Object, null!, null!, null!, null!, null!, null!, null!, null!, null!);

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
        var manager = new GoodsReceiptManager(null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            manager.GetGoodsReceiptsAsync(pageIndex: -1, pageSize: 10,
                keywords: null, fromDateUtc: null, toDateUtc: null));
    }

    [Fact]
    public async Task GetGoodsReceiptsAsync_PageSizeLessThanOrEqualTo0_ThrowsArgumentOutOfRangeException()
    {
        var manager = new GoodsReceiptManager(null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!);

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
            null!, goodsReceiptDataReaderStub.Object, null!, null!, null!, null!, null!, null!, null!, null!, null!);

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
            null!, goodsReceiptDataReaderStub.Object, null!, null!, null!, null!, null!, null!, null!, null!, null!);

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
            null!, goodsReceiptDataReaderStub.Object, null!, null!, null!, null!, null!, null!, null!, null!, null!);

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
            null!, goodsReceiptDataReaderStub.Object, null!, null!, null!, null!, null!, null!, null!, null!, null!);

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
            null!, goodsReceiptDataReaderStub.Object, null!, null!, null!, null!, null!, null!, null!, null!, null!);

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
            null!, goodsReceiptDataReaderStub.Object, null!, null!, null!, null!, null!, null!, null!, null!, null!);

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
            null!, goodsReceiptDataReaderStub.Object, null!, null!, null!, null!, null!, null!, null!, null!, null!);

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
        var manager = new GoodsReceiptManager(null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!);

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
        var manager = new GoodsReceiptManager(null!, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!);

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
            null!, goodsReceiptDataReaderMock.Object, null!, null!, null!, null!, null!, null!, null!, null!, null!);

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
            null!, null!, null!, null!, null!, vendorDataReaderMock.Object, null!, null!, null!);

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
            null!, null!, null!, null!, null!, vendorDataReaderStub.Object, null!, null!, null!);

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
            null!, null!, null!, null!, null!, null!, null!, null!, null!);

        var result = await manager.SetGoodsReceiptVendorAsync(dto);

        Assert.Equal(goodsReceipt.Id, result.UpdatedId);
        goodsReceiptRepositoryMock.Verify();
    }

    #endregion

    // ───────────────────────────────────────────────────────────────────────
    // GetSuggestedPurchaseOrdersAsync
    // ───────────────────────────────────────────────────────────────────────

    #region GetSuggestedPurchaseOrdersAsync

    /// <summary>
    /// Tạo một <see cref="GoodsReceipt"/> đã có received date + nhiều items.
    /// Dùng <see cref="WarehouseSettings.AllowNonWarehouse"/>=true để không phải mock warehouse.
    /// </summary>
    private static async Task<GoodsReceipt> BuildGoodsReceiptWithItemsAsync(
        DateTime receivedOnUtc,
        params (Guid ProductId, decimal? UnitCost, decimal Quantity)[] specs)
    {
        var goodsReceipt = await BuildGoodsReceiptAsync();
        goodsReceipt.SetReceivedDate(receivedOnUtc);

        var warehouseSettings = new WarehouseSettings { AllowNonWarehouse = true };

        foreach (var (productId, unitCost, quantity) in specs)
        {
            var product = new Product(productId, $"product-{productId}");
            var productReaderStub = ProductDataReader.ProductById(productId, product);

            await goodsReceipt.AddItemAsync(
                productId, warehouseId: null, quantity, unitCost,
                productReaderStub.Object, warehouseSettings, warehouseByIdGetter: null!);
        }

        return goodsReceipt;
    }

    /// <summary>
    /// Tạo một <see cref="PurchaseOrder"/> phục vụ kiểm thử logic gợi ý:
    /// builder cho meta data + reflection để add items (bypass Product/Vendor lookup chains)
    /// + ChangeStatus để đưa về status mong muốn.
    /// </summary>
    private static async Task<PurchaseOrder> BuildPurchaseOrderForSuggestionAsync(
        string code, Guid vendorId, DateTime placedOnUtc, PurchaseOrderStatus status,
        params (Guid ProductId, decimal UnitCost, decimal QuantityOrdered, decimal QuantityReceived)[] specs)
    {
        var purchaseOrderByIdGetterMock = new Mock<IGetByIdService<PurchaseOrder>>();
        purchaseOrderByIdGetterMock
            .Setup(g => g.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync((PurchaseOrder)null!);

        var codeCheckerMock = new Mock<ICodeExistCheckingService>();
        codeCheckerMock
            .Setup(c => c.DoesCodeExistAsync(code))
            .ReturnsAsync(false);

        var vendorByIdGetterMock = new Mock<IGetByIdService<Vendor>>();
        vendorByIdGetterMock
            .Setup(g => g.GetByIdAsync(vendorId))
            .ReturnsAsync(new Vendor(vendorId, "vendor-name", "0900000000"));

        var currentUserMock = new Mock<ICurrentUserAccessor>();
        currentUserMock
            .Setup(u => u.GetCurrentUserAsync())
            .ReturnsAsync((CurrentUserInfoDto?)null);

        var purchaseOrder = await PurchaseOrder.CreateBuilder()
            .WithCode(code, codeCheckerMock.Object)
            .WithVendor(vendorId, vendorByIdGetterMock.Object)
            .BuildAsync(purchaseOrderByIdGetterMock.Object, currentUserMock.Object);

        purchaseOrder.SetPlacedDate(placedOnUtc);

        // Thêm items qua reflection vào _items (private List<PurchaseOrderItem>) để bỏ qua
        // các check Product/Vendor của AddPurchaseOrderItemAsync — không liên quan đến nghiệp vụ
        // gợi ý PO.
        var itemsField = typeof(PurchaseOrder)
            .GetField("_items", BindingFlags.Instance | BindingFlags.NonPublic);
        var itemsList = (List<PurchaseOrderItem>)itemsField!.GetValue(purchaseOrder)!;

        foreach (var (productId, unitCost, qtyOrdered, qtyReceived) in specs)
        {
            var item = new PurchaseOrderItem(purchaseOrder.Id, productId, qtyOrdered, unitCost);
            if (qtyReceived > 0)
                item.AddQuantityReceived(qtyReceived);
            itemsList.Add(item);
        }

        if (status != PurchaseOrderStatus.Draft)
            purchaseOrder.ChangeStatus(status);

        return purchaseOrder;
    }

    [Fact]
    public async Task GetSuggestedPurchaseOrdersAsync_ExactMatchOnProductIdAndUnitCost_ReturnsScore100AndIsFullMatch()
    {
        // GR có 10 cái sản phẩm A với UnitCost = 50,000.
        // PO Approved trước ngày nhận, có 20 cái A@50,000 (đủ remaining).
        // → matched = 10, score = 100, IsFullMatch = true.
        var productId = Guid.NewGuid();
        var unitCost = 50_000m;

        var goodsReceipt = await BuildGoodsReceiptWithItemsAsync(
            receivedOnUtc: new DateTime(2026, 4, 20, 0, 0, 0, DateTimeKind.Utc),
            (productId, unitCost, 10m));

        var purchaseOrder = await BuildPurchaseOrderForSuggestionAsync(
            code: "PO-EXACT",
            vendorId: Guid.NewGuid(),
            placedOnUtc: new DateTime(2026, 4, 10, 0, 0, 0, DateTimeKind.Utc),
            status: PurchaseOrderStatus.Approved,
            (productId, unitCost, 20m, 0m));

        var goodsReceiptDataReaderStub = GoodsReceiptDataReader.GoodsReceiptById(goodsReceipt.Id, goodsReceipt);
        var purchaseOrderDataReaderStub = PurchaseOrderDataReader.WithData(purchaseOrder);
        var manager = new GoodsReceiptManager(
            null!, goodsReceiptDataReaderStub.Object,
            null!, null!, null!, null!, null!, null!,
            purchaseOrderDataReaderStub.Object, null!, null!);

        var results = await manager.GetSuggestedPurchaseOrdersAsync(goodsReceipt.Id);

        Assert.Single(results);
        Assert.Equal(100, results[0].MatchScore);
        Assert.True(results[0].IsFullMatch);
        Assert.Equal(purchaseOrder.Id, results[0].PurchaseOrderId);
    }

    [Fact]
    public async Task GetSuggestedPurchaseOrdersAsync_GrItemHasNoUnitCost_PartialMatchByProductIdReturnsScoreLessThan100()
    {
        // GR yêu cầu 10 cái sản phẩm A nhưng chưa định giá (UnitCost = null).
        // PO Approved chỉ còn 4 cái A (5 ordered - 1 received) → matched=4, score = 40, IsFullMatch = false.
        var productId = Guid.NewGuid();

        var goodsReceipt = await BuildGoodsReceiptWithItemsAsync(
            receivedOnUtc: new DateTime(2026, 4, 20, 0, 0, 0, DateTimeKind.Utc),
            (productId, (decimal?)null, 10m));

        var purchaseOrder = await BuildPurchaseOrderForSuggestionAsync(
            code: "PO-PARTIAL",
            vendorId: Guid.NewGuid(),
            placedOnUtc: new DateTime(2026, 4, 10, 0, 0, 0, DateTimeKind.Utc),
            status: PurchaseOrderStatus.Approved,
            (productId, 50_000m, 5m, 1m));

        var goodsReceiptDataReaderStub = GoodsReceiptDataReader.GoodsReceiptById(goodsReceipt.Id, goodsReceipt);
        var purchaseOrderDataReaderStub = PurchaseOrderDataReader.WithData(purchaseOrder);
        var manager = new GoodsReceiptManager(
            null!, goodsReceiptDataReaderStub.Object,
            null!, null!, null!, null!, null!, null!,
            purchaseOrderDataReaderStub.Object, null!, null!);

        var results = await manager.GetSuggestedPurchaseOrdersAsync(goodsReceipt.Id);

        Assert.Single(results);
        Assert.Equal(40, results[0].MatchScore);
        Assert.False(results[0].IsFullMatch);
    }

    [Fact]
    public async Task GetSuggestedPurchaseOrdersAsync_PoPlacedAfterGrReceived_IsExcluded()
    {
        // PO có PlacedOnUtc > GR.ReceivedOnUtc → không thể là PO mà GR nhận hàng cho.
        var productId = Guid.NewGuid();
        var goodsReceipt = await BuildGoodsReceiptWithItemsAsync(
            receivedOnUtc: new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc),
            (productId, 50_000m, 10m));

        var futurePurchaseOrder = await BuildPurchaseOrderForSuggestionAsync(
            code: "PO-FUTURE",
            vendorId: Guid.NewGuid(),
            placedOnUtc: new DateTime(2026, 4, 15, 0, 0, 0, DateTimeKind.Utc),
            status: PurchaseOrderStatus.Approved,
            (productId, 50_000m, 10m, 0m));

        var goodsReceiptDataReaderStub = GoodsReceiptDataReader.GoodsReceiptById(goodsReceipt.Id, goodsReceipt);
        var purchaseOrderDataReaderStub = PurchaseOrderDataReader.WithData(futurePurchaseOrder);
        var manager = new GoodsReceiptManager(
            null!, goodsReceiptDataReaderStub.Object,
            null!, null!, null!, null!, null!, null!,
            purchaseOrderDataReaderStub.Object, null!, null!);

        var results = await manager.GetSuggestedPurchaseOrdersAsync(goodsReceipt.Id);

        Assert.Empty(results);
    }

    [Fact]
    public async Task GetSuggestedPurchaseOrdersAsync_PoStatusIsDraftSubmittedCompletedOrCancelled_IsExcluded()
    {
        // Chỉ PO trong trạng thái Approved hoặc Receiving mới được gợi ý — các trạng thái còn lại bị loại.
        var productId = Guid.NewGuid();
        var goodsReceipt = await BuildGoodsReceiptWithItemsAsync(
            receivedOnUtc: new DateTime(2026, 4, 20, 0, 0, 0, DateTimeKind.Utc),
            (productId, 50_000m, 10m));

        var draftPurchaseOrder = await BuildPurchaseOrderForSuggestionAsync(
            code: "PO-DRAFT", vendorId: Guid.NewGuid(),
            placedOnUtc: new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc),
            status: PurchaseOrderStatus.Draft,
            (productId, 50_000m, 10m, 0m));

        var submittedPurchaseOrder = await BuildPurchaseOrderForSuggestionAsync(
            code: "PO-SUBMITTED", vendorId: Guid.NewGuid(),
            placedOnUtc: new DateTime(2026, 4, 2, 0, 0, 0, DateTimeKind.Utc),
            status: PurchaseOrderStatus.Submitted,
            (productId, 50_000m, 10m, 0m));

        var completedPurchaseOrder = await BuildPurchaseOrderForSuggestionAsync(
            code: "PO-COMPLETED", vendorId: Guid.NewGuid(),
            placedOnUtc: new DateTime(2026, 4, 3, 0, 0, 0, DateTimeKind.Utc),
            status: PurchaseOrderStatus.Completed,
            (productId, 50_000m, 10m, 0m));

        var cancelledPurchaseOrder = await BuildPurchaseOrderForSuggestionAsync(
            code: "PO-CANCELLED", vendorId: Guid.NewGuid(),
            placedOnUtc: new DateTime(2026, 4, 4, 0, 0, 0, DateTimeKind.Utc),
            status: PurchaseOrderStatus.Cancelled,
            (productId, 50_000m, 10m, 0m));

        var goodsReceiptDataReaderStub = GoodsReceiptDataReader.GoodsReceiptById(goodsReceipt.Id, goodsReceipt);
        var purchaseOrderDataReaderStub = PurchaseOrderDataReader.WithData(
            draftPurchaseOrder, submittedPurchaseOrder, completedPurchaseOrder, cancelledPurchaseOrder);
        var manager = new GoodsReceiptManager(
            null!, goodsReceiptDataReaderStub.Object,
            null!, null!, null!, null!, null!, null!,
            purchaseOrderDataReaderStub.Object, null!, null!);

        var results = await manager.GetSuggestedPurchaseOrdersAsync(goodsReceipt.Id);

        Assert.Empty(results);
    }

    [Fact]
    public async Task GetSuggestedPurchaseOrdersAsync_NoCandidatePurchaseOrder_ReturnsEmptyListWithoutThrowing()
    {
        // Không có PO nào trong hệ thống → trả empty list, không throw.
        var productId = Guid.NewGuid();
        var goodsReceipt = await BuildGoodsReceiptWithItemsAsync(
            receivedOnUtc: new DateTime(2026, 4, 20, 0, 0, 0, DateTimeKind.Utc),
            (productId, 50_000m, 10m));

        var goodsReceiptDataReaderStub = GoodsReceiptDataReader.GoodsReceiptById(goodsReceipt.Id, goodsReceipt);
        var purchaseOrderDataReaderStub = PurchaseOrderDataReader.Empty();
        var manager = new GoodsReceiptManager(
            null!, goodsReceiptDataReaderStub.Object,
            null!, null!, null!, null!, null!, null!,
            purchaseOrderDataReaderStub.Object, null!, null!);

        var results = await manager.GetSuggestedPurchaseOrdersAsync(goodsReceipt.Id);

        Assert.NotNull(results);
        Assert.Empty(results);
    }

    [Fact]
    public async Task GetSuggestedPurchaseOrdersAsync_MultiplePoWithDifferentScoresAndDates_OrdersByFullMatchThenScoreThenPlacedDateDesc()
    {
        // Sắp xếp: IsFullMatch desc → MatchScore desc → PlacedOnUtc desc.
        // GR yêu cầu 10 cái A@50K.
        //  - olderFullMatch:  10 ordered, 0 received → matched 10/10 = 100, placed 2026-04-01
        //  - newerFullMatch:  10 ordered, 0 received → matched 10/10 = 100, placed 2026-04-20
        //  - partialMatch:     5 ordered, 0 received → matched  5/10 =  50, placed 2026-04-15
        // Expected order: newerFullMatch → olderFullMatch → partialMatch
        var productId = Guid.NewGuid();
        var unitCost = 50_000m;

        var goodsReceipt = await BuildGoodsReceiptWithItemsAsync(
            receivedOnUtc: new DateTime(2026, 4, 28, 0, 0, 0, DateTimeKind.Utc),
            (productId, unitCost, 10m));

        var olderFullMatch = await BuildPurchaseOrderForSuggestionAsync(
            code: "PO-OLDER-FULL", vendorId: Guid.NewGuid(),
            placedOnUtc: new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc),
            status: PurchaseOrderStatus.Approved,
            (productId, unitCost, 10m, 0m));

        var newerFullMatch = await BuildPurchaseOrderForSuggestionAsync(
            code: "PO-NEWER-FULL", vendorId: Guid.NewGuid(),
            placedOnUtc: new DateTime(2026, 4, 20, 0, 0, 0, DateTimeKind.Utc),
            status: PurchaseOrderStatus.Approved,
            (productId, unitCost, 10m, 0m));

        var partialMatch = await BuildPurchaseOrderForSuggestionAsync(
            code: "PO-PARTIAL", vendorId: Guid.NewGuid(),
            placedOnUtc: new DateTime(2026, 4, 15, 0, 0, 0, DateTimeKind.Utc),
            status: PurchaseOrderStatus.Approved,
            (productId, unitCost, 5m, 0m));

        var goodsReceiptDataReaderStub = GoodsReceiptDataReader.GoodsReceiptById(goodsReceipt.Id, goodsReceipt);
        var purchaseOrderDataReaderStub = PurchaseOrderDataReader.WithData(
            partialMatch, olderFullMatch, newerFullMatch);
        var manager = new GoodsReceiptManager(
            null!, goodsReceiptDataReaderStub.Object,
            null!, null!, null!, null!, null!, null!,
            purchaseOrderDataReaderStub.Object, null!, null!);

        var results = await manager.GetSuggestedPurchaseOrdersAsync(goodsReceipt.Id);

        Assert.Equal(3, results.Count);
        Assert.Equal(newerFullMatch.Id, results[0].PurchaseOrderId);
        Assert.Equal(olderFullMatch.Id, results[1].PurchaseOrderId);
        Assert.Equal(partialMatch.Id, results[2].PurchaseOrderId);
        Assert.True(results[0].IsFullMatch);
        Assert.True(results[1].IsFullMatch);
        Assert.False(results[2].IsFullMatch);
    }

    [Fact]
    public async Task GetSuggestedPurchaseOrdersAsync_PoVendorDifferentFromGr_StillSuggestedWhenItemsMatch()
    {
        // Bộ lọc gợi ý KHÔNG so sánh VendorId — PO khác Vendor với GR vẫn xuất hiện nếu items khớp.
        var productId = Guid.NewGuid();
        var unitCost = 50_000m;

        var goodsReceipt = await BuildGoodsReceiptWithItemsAsync(
            receivedOnUtc: new DateTime(2026, 4, 20, 0, 0, 0, DateTimeKind.Utc),
            (productId, unitCost, 10m));

        var goodsReceiptVendorId = Guid.NewGuid();
        goodsReceipt.SetVendor(goodsReceiptVendorId, "GR Vendor", vendorPhone: null, vendorAddress: null);

        var purchaseOrderVendorId = Guid.NewGuid();
        var purchaseOrder = await BuildPurchaseOrderForSuggestionAsync(
            code: "PO-DIFF-VENDOR",
            vendorId: purchaseOrderVendorId,
            placedOnUtc: new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc),
            status: PurchaseOrderStatus.Approved,
            (productId, unitCost, 10m, 0m));

        Assert.NotEqual(goodsReceipt.VendorId, purchaseOrder.VendorId);

        var goodsReceiptDataReaderStub = GoodsReceiptDataReader.GoodsReceiptById(goodsReceipt.Id, goodsReceipt);
        var purchaseOrderDataReaderStub = PurchaseOrderDataReader.WithData(purchaseOrder);
        var manager = new GoodsReceiptManager(
            null!, goodsReceiptDataReaderStub.Object,
            null!, null!, null!, null!, null!, null!,
            purchaseOrderDataReaderStub.Object, null!, null!);

        var results = await manager.GetSuggestedPurchaseOrdersAsync(goodsReceipt.Id);

        Assert.Single(results);
        Assert.Equal(purchaseOrder.Id, results[0].PurchaseOrderId);
        Assert.Equal(purchaseOrderVendorId, results[0].VendorId);
    }

    #endregion
}
                                                                                                                                                                                                    