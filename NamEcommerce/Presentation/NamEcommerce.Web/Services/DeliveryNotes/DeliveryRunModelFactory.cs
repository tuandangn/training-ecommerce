using NamEcommerce.Application.Contracts.Catalog;
using NamEcommerce.Application.Contracts.DeliveryNotes;
using NamEcommerce.Application.Contracts.Dtos.Catalog;
using NamEcommerce.Application.Contracts.Dtos.DeliveryNotes;
using NamEcommerce.Application.Contracts.Dtos.Inventory;
using NamEcommerce.Application.Contracts.Inventory;
using NamEcommerce.Application.Contracts.Users;
using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Domain.Shared.Enums.DeliveryNotes;
using NamEcommerce.Web.Contracts.Configurations;
using NamEcommerce.Web.Contracts.Models.Common;
using NamEcommerce.Web.Contracts.Models.DeliveryNotes;
using NamEcommerce.Web.Extensions;
using NamEcommerce.Web.Framework.Services;

namespace NamEcommerce.Web.Services.DeliveryNotes;

public sealed class DeliveryRunModelFactory(
    IDeliveryRunAppService deliveryRunAppService,
    IDeliveryNoteAppService deliveryNoteAppService,
    IUserAppService userAppService,
    IProductAppService productAppService,
    IUnitMeasurementAppService unitMeasurementAppService,
    IWarehouseAppService warehouseAppService,
    AppConfig appConfig, ICurrentUserService currentUserService) : IDeliveryRunModelFactory
{
    public async Task<DeliveryRunListModel> PrepareDeliveryRunListModelAsync(DeliveryRunSearchModel searchModel)
    {
        var pageNumber = searchModel.PageNumber ?? 1;
        var pageSize = searchModel.PageSize ?? 0;
        if (pageNumber <= 0) pageNumber = 1;
        if (pageSize <= 0 || !appConfig.PageSizeOptions.Contains(pageSize)) pageSize = appConfig.DefaultPageSize;

        var pagedData = await deliveryRunAppService.GetListAsync(
            pageNumber - 1,
            pageSize,
            searchModel.Keywords,
            searchModel.AssignedDeliveryUserId,
            searchModel.Status).ConfigureAwait(false);

        return new DeliveryRunListModel
        {
            Keywords = searchModel.Keywords,
            AssignedDeliveryUserId = searchModel.AssignedDeliveryUserId,
            Status = searchModel.Status,
            AvailableDeliveryUsers = await PrepareDeliveryUserOptionsAsync().ConfigureAwait(false),
            Data = PagedDataModel.Create(
                await PrepareDeliveryRunListItemsAsync(pagedData.Items).ConfigureAwait(false),
                pagedData.Pagination.PageIndex,
                pagedData.Pagination.PageSize,
                pagedData.Pagination.TotalCount)
        };
    }

    public async Task<CreateDeliveryRunModel> PrepareCreateDeliveryRunModelAsync(CreateDeliveryRunModel? oldModel = null)
    {
        var model = oldModel ?? new CreateDeliveryRunModel();
        model.AvailableDeliveryUsers = await PrepareDeliveryUserOptionsAsync().ConfigureAwait(false);
        model.AvailableDeliveryNotes = await PrepareCandidateDeliveryNotesAsync().ConfigureAwait(false);
        return model;
    }

    public async Task<DeliveryRunDetailsModel?> PrepareDeliveryRunDetailsModelAsync(Guid id)
    {
        var deliveryRun = await deliveryRunAppService.GetByIdAsync(id).ConfigureAwait(false);
        if (deliveryRun is null)
            return null;

        var currentDeliveryNotes = new Dictionary<Guid, DeliveryNoteAppDto>();
        foreach (var item in deliveryRun.Items)
        {
            var deliveryNote = await deliveryNoteAppService.GetByIdAsync(item.DeliveryNoteId).ConfigureAwait(false);
            if (deliveryNote is not null)
                currentDeliveryNotes[item.DeliveryNoteId] = deliveryNote;
        }

        var productIds = currentDeliveryNotes.SelectMany(note => note.Value.Items.Select(item => item.ProductId)).Distinct().ToList();
        var products = await productAppService.GetProductsByIdsAsync(productIds).ConfigureAwait(false);

        var unitMeasurementIds = products.Select(product => product.UnitMeasurementId).OfType<Guid>().Distinct().ToList();
        var unitMeasurements = await unitMeasurementAppService.GetUnitMeasurementsByIdsAsync(unitMeasurementIds).ConfigureAwait(false);

        var warehouseIds = currentDeliveryNotes.SelectMany(note => note.Value.Items.Select(item => item.WarehouseId)).OfType<Guid>().Distinct().ToList();
        var warehouses = (await warehouseAppService.GetWarehousesAsync(0, int.MaxValue).ConfigureAwait(false))
            .Where(warehouse => warehouseIds.Contains(warehouse.Id)).ToList();

        var itemModels = deliveryRun.Items.Select(item => PrepareDeliveryRunItemModel(item)).ToList();
        var progress = GetProgressInfo((DeliveryRunStatus)deliveryRun.Status, itemModels);

        return new DeliveryRunDetailsModel
        {
            Id = deliveryRun.Id,
            Code = deliveryRun.Code,
            AssignedDeliveryUserId = deliveryRun.AssignedDeliveryUserId,
            AssignedDeliveryUsername = deliveryRun.AssignedDeliveryUsername,
            AssignedDeliveryFullName = deliveryRun.AssignedDeliveryFullName,
            Status = deliveryRun.Status,
            DeliveryStatusInfo = progress.StatusName,
            DeliveredItemCount = progress.DeliveredItemCount,
            IsDeliveryCompleted = progress.IsCompleted,
            PreparedByUserId = deliveryRun.PreparedByUserId,
            PreparedOn = DateTimeHelper.ToLocalTime(deliveryRun.PreparedOnUtc),
            HandedOverByUserId = deliveryRun.HandedOverByUserId,
            HandedOverOn = DateTimeHelper.ToLocalTime(deliveryRun.HandedOverOnUtc),
            DriverCachedOn = DateTimeHelper.ToLocalTime(deliveryRun.DriverCachedOnUtc),
            DriverCacheDeviceId = deliveryRun.DriverCacheDeviceId,
            PaperManifestIssued = deliveryRun.PaperManifestIssued,
            PaperManifestIssuedOnUtc = deliveryRun.PaperManifestIssuedOnUtc,
            CashHandoverConfirmedByUserId = deliveryRun.CashHandoverConfirmedByUserId,
            CashHandoverConfirmedByUsername = deliveryRun.CashHandoverConfirmedByUsername,
            CashHandoverConfirmedByFullName = deliveryRun.CashHandoverConfirmedByFullName,
            CashHandoverConfirmedOn = DateTimeHelper.ToLocalTime(deliveryRun.CashHandoverConfirmedOnUtc),
            CashHandoverAmount = deliveryRun.CashHandoverAmount,
            CashHandoverNote = deliveryRun.CashHandoverNote,
            Note = deliveryRun.Note,
            CreatedOn = DateTimeHelper.ToLocalTime(deliveryRun.CreatedOnUtc),
            UpdatedOn = DateTimeHelper.ToLocalTime(deliveryRun.UpdatedOnUtc),
            Items = itemModels,
            PickingManifest = BuildPickingManifest(deliveryRun.WarehousePicks, currentDeliveryNotes.Values.ToList(), products, unitMeasurements, warehouses),
            CanManageDeliveryRun = await currentUserService.IsAdminAsync().ConfigureAwait(false) ||
                await currentUserService.IsWarehouseManager().ConfigureAwait(false),
            CanConfirmDeliveryRunCashHandover = await currentUserService.IsAdminAsync().ConfigureAwait(false) ||
                await currentUserService.IsInRole(SystemUserRoleNames.Cashier).ConfigureAwait(false),
            CanIssuePaperManifest = deliveryRun.CanIssuePaperManifest,
            CanCancel= deliveryRun.CanCancel,
            CanCloseIfDelivered = deliveryRun.CanCloseIfDelivered,
            CanReviewCashCollected = deliveryRun.CanReviewCashCollected,
            CanReconcileDeliveryRunItems = deliveryRun.CanReconcileDeliveryRunItems
        };

        #region Local methods

        DeliveryRunItemModel PrepareDeliveryRunItemModel(DeliveryRunItemAppDto item)
        {
            currentDeliveryNotes.TryGetValue(item.DeliveryNoteId, out var note);
            var productItems = note?.Items.Select(noteItem =>
            {
                var product = products.FirstOrDefault(product => product.Id == noteItem.ProductId);
                var unitMeasurement = product is not null ? unitMeasurements.FirstOrDefault(unitMeasurement => unitMeasurement.Id == product.UnitMeasurementId) : null;
                var warehouse = warehouses.FirstOrDefault(warehouse => warehouse.Id == noteItem.WarehouseId);
                return new DeliveryRunProductItemModel
                {
                    DeliveryNoteItemId = noteItem.Id,
                    ProductId = noteItem.ProductId,
                    ProductName = noteItem.ProductName,
                    WarehouseName = warehouse?.Name,
                    Quantity = noteItem.Quantity,
                    UnitPrice = noteItem.UnitPrice,
                    SubTotal = noteItem.SubTotal,
                    QuantityDecimalPlaces = unitMeasurement?.DecimalPlaces ?? 2,
                    UnitMeasurement = unitMeasurement?.Name ?? string.Empty
                };
            }).ToList() ?? [];
            var settlementItems = new List<DeliveryRunSettlementProductItemModel>();
            if (note is not null)
            {
                var noteItemsById = note.Items.ToDictionary(noteItem => noteItem.Id);
                foreach (var settlementItem in note.SettlementItems)
                {
                    if (!noteItemsById.TryGetValue(settlementItem.DeliveryNoteItemId, out var noteItem))
                        continue;

                    var product = products.FirstOrDefault(product => product.Id == noteItem.ProductId);
                    var unitMeasurement = product is not null ? unitMeasurements.FirstOrDefault(unitMeasurement => unitMeasurement.Id == product.UnitMeasurementId) : null;
                    settlementItems.Add(new DeliveryRunSettlementProductItemModel
                    {
                        DeliveryNoteItemId = noteItem.Id,
                        ProductId = noteItem.ProductId,
                        ProductName = noteItem.ProductName,
                        Quantity = noteItem.Quantity,
                        AcceptedQuantity = settlementItem.AcceptedQuantity,
                        RejectedQuantity = settlementItem.RejectedQuantity,
                        QuantityDecimalPlaces = unitMeasurement?.DecimalPlaces ?? 2,
                        UnitMeasurement = unitMeasurement?.Name ?? string.Empty
                    });
                }
            }

            return new DeliveryRunItemModel
            {
                DeliveryNoteStatus = note?.Status,
                DeliveredOnUtc = note?.DeliveredOnUtc,
                Id = item.Id,
                DeliveryNoteId = item.DeliveryNoteId,
                DeliveryNoteCode = item.DeliveryNoteCode,
                OrderCode = note?.OrderCode ?? item.OrderCode,
                CustomerId = note?.CustomerId ?? Guid.Empty,
                CustomerName = note?.CustomerName ?? item.CustomerName,
                CustomerPhone = string.IsNullOrWhiteSpace(note?.ShippingPhoneNumber)
                    ? item.ShippingPhoneNumber
                    : note?.ShippingPhoneNumber,
                ShippingPhoneNumber = string.IsNullOrWhiteSpace(note?.ShippingPhoneNumber)
                    ? item.ShippingPhoneNumber
                    : note?.ShippingPhoneNumber,
                ShippingAddress = note?.ShippingAddress ?? item.ShippingAddress,
                AmountToCollect = note?.AmountToCollect ?? item.AmountToCollect,
                AmountToCollectOverriddenAt = note?.AmountToCollectOverriddenAt,
                CashCollectedAmount = note?.DeliveryCashCollectedAmount,
                ReceiverName = note?.DeliveryReceiverName,
                DeliveryProofPictureId = note?.DeliveryProofPictureId,
                SettlementApproval = note?.SettlementApproval ?? 0,
                ProposedAmountToCollect = note?.ProposedAmountToCollect,
                ApprovedAmountToCollect = note?.ApprovedAmountToCollect,
                SettlementReason = note?.SettlementReason,
                SettlementAdminNote = note?.SettlementAdminNote,
                ProductItems = productItems,
                ProductLines = BuildProductLines(productItems),
                SettlementItems = settlementItems,
                SettlementProductLines = BuildSettlementProductLines(settlementItems)
            };
        }

        #endregion
    }

    public async Task<DeliveryMobileIndexModel> PrepareDeliveryMobileIndexModelAsync(Guid currentUserId, string currentUserFullName,
        bool showCompleted)
    {
        var pagedData = await deliveryRunAppService.GetListAsync(
            0,
            50,
            null,
            currentUserId,
            null).ConfigureAwait(false);

        var candidateRuns = pagedData.Items
            .Where(run => run.Status != (int)DeliveryRunStatus.Cancelled)
            .Where(run => run.Status != (int)DeliveryRunStatus.Closed)
            .ToList();
        var runs = new List<DeliveryMobileRunListItemModel>();
        foreach (var run in candidateRuns)
        {
            var details = await PrepareDeliveryRunDetailsModelAsync(run.Id).ConfigureAwait(false);
            var model = ToMobileListModel(run, details.Items);
            if (showCompleted != model.IsDeliveryCompleted)
                continue;

            runs.Add(model);
        }

        return new DeliveryMobileIndexModel
        {
            CurrentUserId = currentUserId,
            CurrentUserFullName = currentUserFullName,
            ShowCompleted = showCompleted,
            Runs = runs
        };
    }

    public async Task<DeliveryMobileRunModel> PrepareDeliveryMobileRunModelAsync(Guid id, Guid currentUserId, string currentUserFullName)
    {
        var run = await PrepareDeliveryRunDetailsModelAsync(id).ConfigureAwait(false);
        if (run.AssignedDeliveryUserId != currentUserId)
            throw new UnauthorizedAccessException("Delivery run is not assigned to current user");

        return new DeliveryMobileRunModel
        {
            CurrentUserId = currentUserId,
            CurrentUserFullName = currentUserFullName,
            Run = run
        };
    }

    private async Task<EntityOptionListModel> PrepareDeliveryUserOptionsAsync()
    {
        var users = await userAppService.GetUsersByRoleAsync(SystemUserRoleNames.DeliveryStaff).ConfigureAwait(false);
        return new EntityOptionListModel
        {
            Options = users.Select(user => new EntityOptionListModel.EntityOptionModel
            {
                Id = user.Id,
                Name = string.IsNullOrWhiteSpace(user.FullName)
                    ? user.Username
                    : $"{user.FullName} ({user.Username})"
            }).ToList()
        };
    }

    private async Task<IList<DeliveryRunCandidateDeliveryNoteModel>> PrepareCandidateDeliveryNotesAsync()
    {
        var notes = await deliveryNoteAppService.GetDeliveryNotesAsync(0, 500, null).ConfigureAwait(false);
        var activeDeliveryNoteIds = await GetActiveDeliveryRunNoteIdsAsync().ConfigureAwait(false);

        return notes.Items
            .Where(note => note.Status == (int)DeliveryNoteStatus.Confirmed)
            .Where(note => !note.IsDirectShip && note.SourceType == (int)DeliveryNoteSourceType.ToCustomer)
            .Where(note => !activeDeliveryNoteIds.Contains(note.Id))
            .OrderBy(note => note.CreatedOnUtc)
            .Select(note => new DeliveryRunCandidateDeliveryNoteModel
            {
                Id = note.Id,
                Code = note.Code,
                OrderCode = note.OrderCode,
                CustomerName = note.CustomerName,
                ShippingPhoneNumber = string.IsNullOrWhiteSpace(note.ShippingPhoneNumber)
                    ? note.CustomerPhone
                    : note.ShippingPhoneNumber,
                ShippingAddress = note.ShippingAddress,
                AssignedDeliveryFullName = note.AssignedDeliveryFullName,
                ProductSummary = BuildProductSummary(note.Items),
                TotalQuantity = note.Items.Sum(item => item.Quantity),
                AmountToCollect = note.AmountToCollect,
                CreatedOnUtc = note.CreatedOnUtc
            })
            .ToList();
    }

    private async Task<HashSet<Guid>> GetActiveDeliveryRunNoteIdsAsync()
    {
        var runs = await deliveryRunAppService.GetListAsync(0, 500, null, null, null).ConfigureAwait(false);
        var activeRuns = runs.Items
            .Where(run => run.Status != (int)DeliveryRunStatus.Closed && run.Status != (int)DeliveryRunStatus.Cancelled)
            .ToList();

        var noteIds = new HashSet<Guid>();
        foreach (var run in activeRuns)
        {
            var details = await deliveryRunAppService.GetByIdAsync(run.Id).ConfigureAwait(false);
            if (details is null)
                continue;

            foreach (var item in details.Items)
                noteIds.Add(item.DeliveryNoteId);
        }

        return noteIds;
    }

    private static string BuildProductSummary(IEnumerable<DeliveryNoteItemAppDto> items)
    {
        var itemList = items.ToList();
        var summary = string.Join(", ", itemList
            .Take(2)
            .Select(item => $"{item.ProductName} x {item.Quantity.DisplayQuantity()}"));
        if (itemList.Count <= 2)
            return summary;

        return $"{summary} +{itemList.Count - 2}";
    }

    private async Task<IList<DeliveryRunListItemModel>> PrepareDeliveryRunListItemsAsync(IEnumerable<DeliveryRunListAppDto> runs)
    {
        var items = new List<DeliveryRunListItemModel>();
        foreach (var run in runs)
        {
            var details = await PrepareDeliveryRunDetailsModelAsync(run.Id).ConfigureAwait(false);
            items.Add(ToListModel(run, details.Items));
        }

        return items;
    }

    private static DeliveryRunListItemModel ToListModel(DeliveryRunListAppDto run, IList<DeliveryRunItemModel> items)
    {
        var progress = GetProgressInfo((DeliveryRunStatus)run.Status, items);

        return new()
        {
            Id = run.Id,
            Code = run.Code,
            AssignedDeliveryUserId = run.AssignedDeliveryUserId,
            AssignedDeliveryFullName = run.AssignedDeliveryFullName,
            Status = run.Status,
            StatusName = progress.StatusName,
            ItemCount = run.ItemCount,
            DeliveredItemCount = progress.DeliveredItemCount,
            IsDeliveryCompleted = progress.IsCompleted,
            AmountToCollect = run.AmountToCollect,
            DriverCachedOnUtc = run.DriverCachedOnUtc,
            PaperManifestIssued = run.PaperManifestIssued,
            HandedOverOnUtc = run.HandedOverOnUtc,
            CashHandoverConfirmedOnUtc = run.CashHandoverConfirmedOnUtc,
            CashHandoverAmount = run.CashHandoverAmount,
            CreatedOnUtc = run.CreatedOnUtc
        };
    }

    private static DeliveryMobileRunListItemModel ToMobileListModel(DeliveryRunListAppDto run, IList<DeliveryRunItemModel> items)
    {
        var progress = GetProgressInfo((DeliveryRunStatus)run.Status, items);

        return new()
        {
            Id = run.Id,
            Code = run.Code,
            Status = run.Status,
            StatusName = progress.StatusName,
            ItemCount = run.ItemCount,
            DeliveredItemCount = progress.DeliveredItemCount,
            IsDeliveryCompleted = progress.IsCompleted,
            AmountToCollect = GetPendingCashAmount(items),
            DriverCachedOnUtc = run.DriverCachedOnUtc,
            PaperManifestIssued = run.PaperManifestIssued,
            HandedOverOnUtc = run.HandedOverOnUtc,
            CreatedOnUtc = run.CreatedOnUtc,
            Items = items
        };
    }

    private static DeliveryRunProgressInfo GetProgressInfo(DeliveryRunStatus status, IList<DeliveryRunItemModel> items)
    {
        var deliveredCount = items.Count(item => item.DeliveryNoteStatus == (int)DeliveryNoteStatus.Delivered);
        var openCount = items.Count(item =>
            item.DeliveryNoteStatus != (int)DeliveryNoteStatus.Delivered &&
            item.DeliveryNoteStatus != (int)DeliveryNoteStatus.Cancelled);
        var isCompleted = items.Count > 0 && openCount == 0;
        var pendingConfirmationCount = items.Count(item => item.DeliveryNoteStatus == (int)DeliveryNoteStatus.PendingConfirmation);
        var hasStarted = deliveredCount > 0 ||
            pendingConfirmationCount > 0 ||
            items.Any(item => item.DeliveryNoteStatus == (int)DeliveryNoteStatus.Delivering);
        var statusName = status switch
        {
            DeliveryRunStatus.Cancelled => "Đã hủy",
            _ when isCompleted => "Đã giao xong",
            _ when pendingConfirmationCount > 0 => "Chờ đối soát",
            _ when hasStarted || status == DeliveryRunStatus.HandedToDriver => "Đang giao",
            DeliveryRunStatus.ReadyForHandover => "Chờ bàn giao",
            DeliveryRunStatus.Closed => "Đã đóng",
            DeliveryRunStatus.Planning => "Đang lập",
            _ => status.ToString()
        };

        return new DeliveryRunProgressInfo(statusName, deliveredCount, isCompleted);
    }

    private static decimal GetPendingCashAmount(IEnumerable<DeliveryRunItemModel> items)
        => items
            .Where(item => item.DeliveryNoteStatus != (int)DeliveryNoteStatus.Delivered &&
                           item.DeliveryNoteStatus != (int)DeliveryNoteStatus.Cancelled)
            .Sum(item => item.AmountToCollect);

    private static IList<DeliveryRunWarehousePickGroupModel> BuildPickingManifest(
        IEnumerable<DeliveryRunWarehousePickAppDto> warehousePicks,
        IEnumerable<DeliveryNoteAppDto> notes,
        IEnumerable<ProductAppDto> products,
        IEnumerable<UnitMeasurementAppDto> unitMeasurements,
        IEnumerable<WarehouseAppDto> warehouses)
    {
        var confirmedByWarehouse = warehousePicks.ToDictionary(p => p.WarehouseId);
        var warehouseList = warehouses.ToList();
        var productList = products.ToList();
        var unitMeasurementList = unitMeasurements.ToList();
        var noteItemsByWarehouse = notes
            .SelectMany(note => note.Items)
            .GroupBy(item => item.WarehouseId)
            .ToDictionary(g => g.Key, g => g.ToList());

        return noteItemsByWarehouse
            .OrderBy(kv => warehouseList.FirstOrDefault(w => w.Id == kv.Key)?.Name ?? kv.Key.ToString())
            .Select(kv =>
            {
                var warehouseId = kv.Key;
                var warehouse = warehouseList.FirstOrDefault(w => w.Id == warehouseId);
                confirmedByWarehouse.TryGetValue(warehouseId, out var pick);

                var productLines = kv.Value
                    .GroupBy(item => item.ProductId)
                    .Select(group =>
                    {
                        var first = group.First();
                        var product = productList.FirstOrDefault(p => p.Id == first.ProductId);
                        var unitMeasurement = product is not null
                            ? unitMeasurementList.FirstOrDefault(u => u.Id == product.UnitMeasurementId)
                            : null;
                        return new DeliveryRunProductLineModel
                        {
                            ProductId = first.ProductId,
                            ProductName = first.ProductName,
                            TotalQuantity = group.Sum(item => item.Quantity),
                            QuantityDecimalPlaces = unitMeasurement?.DecimalPlaces ?? 2,
                            UnitMeasurement = unitMeasurement?.Name ?? string.Empty
                        };
                    })
                    .ToList();

                return new DeliveryRunWarehousePickGroupModel
                {
                    WarehouseId = warehouseId,
                    WarehouseName = warehouse?.Name ?? warehouseId.ToString(),
                    Confirmed = pick is not null,
                    ConfirmedByFullName = pick?.ConfirmedByFullName,
                    ConfirmedOnUtc = pick?.ConfirmedOnUtc,
                    Products = productLines
                };
            })
            .ToList();
    }

    private static IList<DeliveryRunProductLineModel> BuildProductLines(IList<DeliveryRunProductItemModel> items)
        => items
            .GroupBy(item => item.ProductId)
            .Select(group =>
            {
                var first = group.First();
                return new DeliveryRunProductLineModel
                {
                    ProductId = group.Key,
                    ProductName = first.ProductName,
                    TotalQuantity = group.Sum(item => item.Quantity),
                    QuantityDecimalPlaces = first.QuantityDecimalPlaces,
                    UnitMeasurement = first.UnitMeasurement
                };
            })
            .ToList();

    private static IList<DeliveryRunSettlementProductLineModel> BuildSettlementProductLines(
        IList<DeliveryRunSettlementProductItemModel> items)
        => items
            .GroupBy(item => item.ProductId)
            .Select(group =>
            {
                var first = group.First();
                return new DeliveryRunSettlementProductLineModel
                {
                    ProductId = group.Key,
                    ProductName = first.ProductName,
                    Quantity = group.Sum(item => item.Quantity),
                    AcceptedQuantity = group.Sum(item => item.AcceptedQuantity),
                    RejectedQuantity = group.Sum(item => item.RejectedQuantity),
                    QuantityDecimalPlaces = first.QuantityDecimalPlaces,
                    UnitMeasurement = first.UnitMeasurement
                };
            })
            .ToList();

    private sealed record DeliveryRunProgressInfo(
        string StatusName,
        int DeliveredItemCount,
        bool IsCompleted);
}
