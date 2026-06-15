using NamEcommerce.Application.Contracts.CustomerPortal;
using NamEcommerce.Application.Contracts.Debts;
using NamEcommerce.Application.Contracts.DeliveryNotes;
using NamEcommerce.Application.Contracts.Dtos.CustomerPortal;
using NamEcommerce.Application.Contracts.Dtos.Media;
using NamEcommerce.Application.Contracts.Dtos.Orders;
using NamEcommerce.Application.Contracts.Media;
using NamEcommerce.Application.Contracts.Notifications;
using NamEcommerce.Application.Contracts.Orders;
using NamEcommerce.Application.Contracts.Returns;
using NamEcommerce.Application.Services.Notifications;
using NamEcommerce.Domain.Entities.Catalog;
using NamEcommerce.Domain.Entities.Customers;
using NamEcommerce.Domain.Entities.DeliveryNotes;
using NamEcommerce.Domain.Entities.Inventory;
using NamEcommerce.Domain.Entities.Orders;
using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Domain.Shared.Dtos.CustomerPortal;
using NamEcommerce.Domain.Shared.Dtos.DeliveryNotes;
using NamEcommerce.Domain.Shared.Enums.CustomerPortal;
using NamEcommerce.Domain.Shared.Enums.DeliveryNotes;
using NamEcommerce.Domain.Shared.Enums.Orders;
using NamEcommerce.Domain.Shared.Exceptions;
using NamEcommerce.Domain.Shared.Services.CustomerPortal;
using NamEcommerce.Domain.Shared.Services.Debts;
using NamEcommerce.Domain.Shared.Services.DeliveryNotes;

namespace NamEcommerce.Application.Services.CustomerPortal;

public sealed class CustomerPortalAppService(
    ICustomerPortalSecurityManager securityManager,
    ICustomerPortalManager customerPortalManager,
    IDeliveryNoteAppService deliveryNoteAppService,
    IDeliveryNoteManager deliveryNoteManager,
    ICustomerDebtAppService customerDebtAppService,
    ICustomerLedgerManager customerLedgerManager,
    IOrderAppService orderAppService,
    ICustomerReturnAppService customerReturnAppService,
    IEntityDataReader<Order> orderReader,
    IEntityDataReader<DeliveryNote> deliveryNoteReader,
    IEntityDataReader<Product> productReader,
    IEntityDataReader<Category> categoryReader,
    IEntityDataReader<Customer> customerReader,
    IEntityDataReader<Warehouse> warehouseReader,
    IPictureAppService pictureAppService,
    ISystemNotificationAppService systemNotificationAppService,
    CustomerPortalStoreOptions storeOptions) : ICustomerPortalAppService
{
    private const int DefaultProductPageSize = 30;
    private const int MaxProductPageSize = 40;
    private const int MaxCategoryCount = 80;
    private const int MaxKeywordLength = 80;
    private const int MaxReturnEvidencePicturesPerItem = 3;
    private const int MaxReturnEvidencePictureBytes = 5 * 1024 * 1024;
    private const string OrderRequestRelatedEntityType = "CustomerOrderRequest";
    private const string ReturnRequestRelatedEntityType = "CustomerReturnRequest";
    private const string DeliveryNoteRelatedEntityType = "DeliveryNote";
    private static readonly HashSet<string> AllowedReturnEvidenceMimeTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/png",
        "image/webp"
    };

    public async Task<PublicDeliveryNoteAppDto?> GetPublicDeliveryNoteByTokenAsync(string token)
    {
        var tokenDto = await securityManager.ResolveDeliveryNoteAccessTokenAsync(CustomerPortalHashing.Hash(token), DateTime.UtcNow).ConfigureAwait(false);
        if (tokenDto is null)
            return null;

        var deliveryNote = await deliveryNoteAppService.GetByIdAsync(tokenDto.DeliveryNoteId).ConfigureAwait(false);
        if (deliveryNote is null)
            return null;

        await securityManager.MarkDeliveryNoteAccessTokenViewedAsync(tokenDto.Id, DateTime.UtcNow).ConfigureAwait(false);

        return new PublicDeliveryNoteAppDto
        {
            Id = deliveryNote.Id,
            Code = deliveryNote.Code,
            OrderCode = deliveryNote.OrderCode,
            Status = deliveryNote.Status,
            DeliveryConfirmationStatus = deliveryNote.DeliveryConfirmationStatus,
            CreatedOnUtc = deliveryNote.CreatedOnUtc,
            DeliveredOnUtc = deliveryNote.DeliveredOnUtc,
            Items = deliveryNote.Items.Select(item => new PublicDeliveryNoteItemAppDto
            {
                Id = item.Id,
                ProductId = item.ProductId,
                ProductName = item.ProductName,
                Quantity = item.Quantity
            }).ToList()
        };
    }

    public async Task<CustomerDashboardAppDto> GetDashboardAsync(Guid customerId)
        => new()
        {
            RecentOrders = (await GetOrdersAsync(customerId).ConfigureAwait(false)).Take(5).ToList(),
            RecentDeliveryNotes = (await GetDeliveryNotesAsync(customerId).ConfigureAwait(false)).Take(5).ToList(),
            DebtSummary = await GetDebtSummaryAsync(customerId).ConfigureAwait(false)
        };

    public Task<IReadOnlyCollection<CustomerOrderSummaryAppDto>> GetOrdersAsync(Guid customerId)
    {
        var orders = orderReader.DataSource
            .Where(order => order.CustomerId == customerId)
            .OrderByDescending(order => order.CreatedOnUtc)
            .ToList()
            .Select(MapOrderSummary)
            .ToList();

        return Task.FromResult<IReadOnlyCollection<CustomerOrderSummaryAppDto>>(orders);
    }

    public Task<CustomerOrderDetailsAppDto?> GetOrderDetailsAsync(Guid customerId, Guid orderId)
    {
        var order = orderReader.DataSource.FirstOrDefault(order => order.Id == orderId && order.CustomerId == customerId);
        if (order is null)
            return Task.FromResult<CustomerOrderDetailsAppDto?>(null);

        var productNames = productReader.DataSource
            .Where(product => order.OrderItems.Select(item => item.ProductId).Contains(product.Id))
            .ToDictionary(product => product.Id, product => product.Name);

        return Task.FromResult<CustomerOrderDetailsAppDto?>(new CustomerOrderDetailsAppDto
        {
            Id = order.Id,
            Code = order.Code,
            Status = (int)order.OrderStatus,
            TotalAmount = order.OrderTotal,
            CreatedOnUtc = order.CreatedOnUtc,
            ExpectedShippingDateUtc = order.ExpectedShippingDateUtc,
            ShippingAddress = order.ShippingAddress,
            Note = order.Note,
            CanUpdateNote = order.OrderStatus is not (OrderStatus.Completed or OrderStatus.Cancelled),
            Items = order.OrderItems.Select(item => new CustomerOrderItemAppDto
            {
                Id = item.Id,
                ProductId = item.ProductId,
                ProductName = productNames.GetValueOrDefault(item.ProductId) ?? string.Empty,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                SubTotal = item.SubTotal
            }).ToList()
        });
    }

    public async Task<CustomerOrderRequestAppDto> CreateOrderRequestAsync(Guid customerId, CreateCustomerOrderRequestAppDto dto)
    {
        var productIds = dto.Items.Select(item => item.ProductId).Distinct().ToList();
        var products = productReader.DataSource
            .Where(product => productIds.Contains(product.Id))
            .ToDictionary(product => product.Id);
        if (products.Count != productIds.Count)
            throw new InvalidOperationException("Error.CustomerPortal.OrderRequest.UnknownProducts");
        var latestPrices = GetLatestPurchasedPrices(customerId, productIds);

        var domainDto = new CreateCustomerOrderRequestDto
        {
            CustomerId = customerId,
            ExpectedShippingDateUtc = dto.ExpectedShippingDateUtc,
            ShippingAddress = dto.ShippingAddress,
            Note = dto.Note,
            Items = dto.Items.Select(item =>
            {
                var product = products.GetValueOrDefault(item.ProductId);
                return new CreateCustomerOrderRequestItemDto
                {
                    ProductId = item.ProductId,
                    ProductName = product?.Name ?? string.Empty,
                    Quantity = item.Quantity,
                    UnitPriceSnapshot = latestPrices.GetValueOrDefault(item.ProductId)
                };
            }).ToList()
        };

        var created = await customerPortalManager.CreateOrderRequestAsync(domainDto).ConfigureAwait(false);
        await systemNotificationAppService
            .CreateAsync(CustomerPortalSystemNotificationComposer.OrderRequestCreated(created))
            .ConfigureAwait(false);
        return new CustomerOrderRequestAppDto
        {
            Id = created.Id,
            Code = created.Code,
            Status = (int)created.Status,
            CreatedOnUtc = created.CreatedOnUtc
        };
    }

    public async Task<IReadOnlyCollection<CustomerOrderRequestSummaryAppDto>> GetOrderRequestsAsync(Guid customerId)
    {
        var requests = await customerPortalManager.GetOrderRequestsAsync(customerId).ConfigureAwait(false);
        return requests.Select(MapOrderRequestSummary).ToList();
    }

    public async Task<CustomerOrderRequestDetailsAppDto?> GetOrderRequestDetailsAsync(Guid customerId, Guid orderRequestId)
    {
        var request = await customerPortalManager.GetOrderRequestByIdAsync(orderRequestId).ConfigureAwait(false);
        if (request is null || request.CustomerId != customerId)
            return null;

        return MapOrderRequestDetails(request);
    }

    public async Task<CustomerPortalConversionResultAppDto> ConfirmOrderRequestAsync(Guid customerId, Guid orderRequestId)
    {
        var request = await customerPortalManager.GetOrderRequestByIdAsync(orderRequestId).ConfigureAwait(false);
        if (request is null || request.CustomerId != customerId)
            return CustomerPortalConversionResultAppDto.Fail("Error.CustomerPortal.OrderRequest.NotFound");

        if (request.Status is not CustomerOrderRequestStatus.Approved)
            return CustomerPortalConversionResultAppDto.Fail("Error.CustomerPortal.OrderRequest.NotApproved");

        if (!IsOrderRequestPriced(request))
            return CustomerPortalConversionResultAppDto.Fail("Error.CustomerPortal.OrderRequest.NotFullyPriced");

        var customer = await customerReader.GetByIdAsync(request.CustomerId, default).ConfigureAwait(false);
        var createDto = new CreateOrderAppDto
        {
            CustomerId = request.CustomerId,
            ExpectedShippingDateUtc = request.ExpectedShippingDateUtc,
            ShippingAddress = request.ShippingAddress,
            ShippingPhoneNumber = customer?.PhoneNumber,
            Note = BuildConvertedOrderNote(request, "Khách đã xác nhận báo giá trên portal.")
        };

        foreach (var item in request.Items)
        {
            createDto.Items.Add(new CreateOrderAppDto.OrderItemAppDto
            {
                ProductId = item.ProductId,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPriceSnapshot
            });
        }

        var result = await orderAppService.CreateOrderAsync(createDto).ConfigureAwait(false);
        if (!result.Success || !result.CreatedId.HasValue)
            return CustomerPortalConversionResultAppDto.Fail(result.ErrorMessage ?? "Error.CustomerPortal.OrderRequest.CreateOrderFailed");

        await customerPortalManager.MarkOrderRequestConvertedAsync(request.Id, result.CreatedId.Value, DateTime.UtcNow).ConfigureAwait(false);
        return CustomerPortalConversionResultAppDto.Ok(result.CreatedId.Value, "Msg.CustomerPortal.OrderRequest.ConfirmedAndOrderCreated");
    }

    public Task<CustomerOrderRequestDefaultsAppDto> GetOrderRequestDefaultsAsync(Guid customerId)
    {
        var latestShippingAddress = orderReader.DataSource
            .Where(order => order.CustomerId == customerId && order.ShippingAddress != null && order.ShippingAddress != string.Empty)
            .OrderByDescending(order => order.CreatedOnUtc)
            .Select(order => order.ShippingAddress)
            .FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(latestShippingAddress))
        {
            return Task.FromResult(new CustomerOrderRequestDefaultsAppDto
            {
                ShippingAddress = latestShippingAddress,
                ShippingAddressSource = "Địa chỉ đơn hàng gần nhất"
            });
        }

        var customer = customerReader.DataSource.FirstOrDefault(customer => customer.Id == customerId);
        var customerAddress = NullIfWhiteSpace(customer?.Address.ToString());

        return Task.FromResult(new CustomerOrderRequestDefaultsAppDto
        {
            ShippingAddress = customerAddress,
            ShippingAddressSource = customerAddress is null ? null : "Địa chỉ khách hàng"
        });
    }

    public async Task<CustomerProductListAppDto> GetProductsAsync(Guid customerId, Guid? categoryId, string? keywords, bool purchasedOnly, int pageSize)
    {
        var safePageSize = Math.Clamp(pageSize <= 0 ? DefaultProductPageSize : pageSize, 1, MaxProductPageSize);
        var query = productReader.DataSource.Where(product => product.Name != null && product.Name != string.Empty);

        if (purchasedOnly)
        {
            query = query.Where(product => orderReader.DataSource.Any(order =>
                order.CustomerId == customerId &&
                order.OrderStatus != OrderStatus.Cancelled &&
                order.OrderItems.Any(item => item.ProductId == product.Id)));
        }

        if (categoryId.HasValue)
        {
            var selectedCategoryId = categoryId.Value;
            query = query.Where(product => product.ProductCategories.Any(category => category.CategoryId == selectedCategoryId));
        }

        if (!string.IsNullOrWhiteSpace(keywords))
        {
            var normalizedKeywords = keywords.Trim();
            if (normalizedKeywords.Length > MaxKeywordLength)
                normalizedKeywords = normalizedKeywords[..MaxKeywordLength];

            normalizedKeywords = normalizedKeywords.ToUpperInvariant();
            query = query.Where(product =>
                product.Name.ToUpper().Contains(normalizedKeywords) ||
                (product.ShortDesc != null && product.ShortDesc.ToUpper().Contains(normalizedKeywords)));
        }

        var products = query
            .OrderBy(product => product.Name)
            .Select(product => new CustomerPortalProductListItem(
                product.Id,
                product.Name,
                product.ProductCategories
                    .OrderBy(category => category.DisplayOrder)
                    .Select(category => (Guid?)category.CategoryId)
                    .FirstOrDefault(),
                product.ProductPictures
                    .OrderBy(picture => picture.DisplayOrder)
                    .Select(picture => (Guid?)picture.PictureId)
                    .FirstOrDefault()))
            .Take(safePageSize + 1)
            .ToList();
        var hasMore = products.Count > safePageSize;
        if (hasMore)
            products = products.Take(safePageSize).ToList();

        var productIds = products.Select(product => product.Id).ToList();
        var latestPrices = productIds.Count == 0
            ? new Dictionary<Guid, decimal>()
            : orderReader.DataSource
                .Where(order => order.CustomerId == customerId && order.OrderStatus != OrderStatus.Cancelled)
                .SelectMany(order => order.OrderItems.Select(item => new
                {
                    item.ProductId,
                    item.UnitPrice,
                    order.CreatedOnUtc
                }))
                .Where(item => productIds.Contains(item.ProductId))
                .OrderByDescending(item => item.CreatedOnUtc)
                .ToList()
                .GroupBy(item => item.ProductId)
                .ToDictionary(group => group.Key, group => group.First().UnitPrice);

        var categoryIds = products
            .Select(product => product.CategoryId)
            .Where(categoryId => categoryId.HasValue)
            .Select(categoryId => categoryId!.Value)
            .Distinct()
            .ToList();
        var categories = categoryReader.DataSource
            .Where(category => categoryIds.Contains(category.Id))
            .ToDictionary(category => category.Id, category => category.Name);
        var items = new List<CustomerProductAppDto>();

        foreach (var product in products)
        {
            var hasPurchased = latestPrices.TryGetValue(product.Id, out var latestUnitPrice);
            string? pictureUrl = null;
            if (product.PictureId.HasValue)
            {
                var picture = await pictureAppService.GetBase64PictureByIdAsync(product.PictureId.Value).ConfigureAwait(false);
                pictureUrl = picture?.Base64Value;
            }

            items.Add(new CustomerProductAppDto
            {
                Id = product.Id,
                Name = product.Name,
                CategoryId = product.CategoryId,
                CategoryName = product.CategoryId.HasValue ? categories.GetValueOrDefault(product.CategoryId.Value) : null,
                PictureUrl = pictureUrl,
                UnitPrice = hasPurchased ? latestUnitPrice : null,
                HasPurchased = hasPurchased
            });
        }

        return new CustomerProductListAppDto
        {
            Items = items,
            HasMore = hasMore,
            PageSize = safePageSize
        };
    }

    public Task<CustomerProductCategoryListAppDto> GetProductCategoriesAsync()
    {
        var categories = categoryReader.DataSource
            .OrderBy(category => category.DisplayOrder)
            .ThenBy(category => category.Name)
            .Take(MaxCategoryCount)
            .ToList()
            .Select(category => new CustomerProductCategoryAppDto
            {
                Id = category.Id,
                Name = category.Name,
                ParentId = category.ParentId
            })
            .ToList();

        return Task.FromResult(new CustomerProductCategoryListAppDto { Items = categories });
    }

    public Task<CustomerContactAppDto> GetContactAsync()
    {
        var warehouses = warehouseReader.DataSource
            .Where(warehouse => warehouse.IsActive)
            .OrderBy(warehouse => warehouse.Name)
            .ToList();
        var fallbackWarehouse = warehouses.FirstOrDefault(warehouse =>
            !string.IsNullOrWhiteSpace(warehouse.Address) || !string.IsNullOrWhiteSpace(warehouse.PhoneNumber));
        var storeName = string.IsNullOrWhiteSpace(storeOptions.StoreName) ? "VLXD Tuấn Khôi" : storeOptions.StoreName.Trim();
        var storePhone = NullIfWhiteSpace(storeOptions.PhoneNumber) ?? fallbackWarehouse?.PhoneNumber;
        var storeAddress = NullIfWhiteSpace(storeOptions.Address) ?? fallbackWarehouse?.Address;
        var storeMapQuery = NullIfWhiteSpace(storeOptions.MapQuery) ?? storeAddress ?? storeName;

        return Task.FromResult(new CustomerContactAppDto
        {
            Store = new CustomerStoreContactAppDto
            {
                StoreName = storeName,
                PhoneNumber = storePhone,
                Address = storeAddress,
                Email = NullIfWhiteSpace(storeOptions.Email),
                MapQuery = storeMapQuery
            },
            Warehouses = warehouses.Select(warehouse => new CustomerWarehouseContactAppDto
            {
                Id = warehouse.Id,
                Name = warehouse.Name,
                PhoneNumber = NullIfWhiteSpace(warehouse.PhoneNumber),
                Address = NullIfWhiteSpace(warehouse.Address),
                MapQuery = NullIfWhiteSpace(warehouse.Address) ?? warehouse.Name
            }).ToList()
        });
    }

    public Task<IReadOnlyCollection<CustomerDeliveryNoteSummaryAppDto>> GetDeliveryNotesAsync(Guid customerId)
    {
        var notes = deliveryNoteReader.DataSource
            .Where(note => note.CustomerId == customerId)
            .OrderByDescending(note => note.CreatedOnUtc)
            .ToList()
            .Select(MapDeliveryNoteSummary)
            .ToList();

        return Task.FromResult<IReadOnlyCollection<CustomerDeliveryNoteSummaryAppDto>>(notes);
    }

    public async Task<CustomerDeliveryNoteDetailsAppDto?> GetDeliveryNoteDetailsAsync(Guid customerId, Guid deliveryNoteId)
    {
        var deliveryNote = deliveryNoteReader.DataSource.FirstOrDefault(note => note.Id == deliveryNoteId && note.CustomerId == customerId);
        if (deliveryNote is null)
            return null;

        var returnStates = await GetDeliveryItemReturnStatesAsync(customerId, deliveryNoteId).ConfigureAwait(false);

        return new CustomerDeliveryNoteDetailsAppDto
        {
            Id = deliveryNote.Id,
            Code = deliveryNote.Code,
            OrderCode = deliveryNote.OrderCode,
            Status = (int)deliveryNote.Status,
            DeliveryConfirmationStatus = (int)deliveryNote.DeliveryConfirmationStatus,
            CreatedOnUtc = deliveryNote.CreatedOnUtc,
            DeliveredOnUtc = deliveryNote.DeliveredOnUtc,
            Items = deliveryNote.Items.Select(item =>
            {
                returnStates.TryGetValue(item.Id, out var returnState);
                return new CustomerDeliveryNoteItemAppDto
                {
                    Id = item.Id,
                    ProductId = item.ProductId,
                    ProductName = item.ProductName ?? string.Empty,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    SubTotal = item.SubTotal,
                    ReservedReturnQuantity = returnState?.ReservedReturnQuantity ?? 0m,
                    PendingPortalReturnQuantity = returnState?.PendingPortalReturnQuantity ?? 0m,
                    ReturnableQuantity = returnState?.ReturnableQuantity ?? item.Quantity
                };
            }).ToList()
        };
    }

    public async Task<CustomerActionResultAppDto> ConfirmDeliveryNoteAsync(Guid customerId, Guid deliveryNoteId, ConfirmCustomerDeliveryNoteAppDto dto)
    {
        var deliveryNote = deliveryNoteReader.DataSource.FirstOrDefault(note => note.Id == deliveryNoteId && note.CustomerId == customerId);
        if (deliveryNote is null)
            return CustomerActionResultAppDto.Fail("Error.DeliveryNoteNotFound");

        var deliveryItemsById = deliveryNote.Items.ToDictionary(item => item.Id);
        var requestItemsByDeliveryItemId = new Dictionary<Guid, ConfirmCustomerDeliveryAcceptanceItemAppDto>();
        if (dto.Acceptance?.Items is not null)
        {
            foreach (var requestItem in dto.Acceptance.Items)
            {
                if (requestItem.DeliveryNoteItemId == Guid.Empty ||
                    !deliveryItemsById.ContainsKey(requestItem.DeliveryNoteItemId) ||
                    !requestItemsByDeliveryItemId.TryAdd(requestItem.DeliveryNoteItemId, requestItem))
                {
                    return CustomerActionResultAppDto.Fail("Error.DeliveryAcceptance.InvalidItem");
                }
            }
        }

        var rejectedItems = new List<(Guid DeliveryNoteItemId, Guid ProductId, decimal RejectedQuantity)>();
        foreach (var deliveryItem in deliveryNote.Items)
        {
            if (!requestItemsByDeliveryItemId.TryGetValue(deliveryItem.Id, out var requestItem))
                continue;

            var acceptedQuantity = requestItem.AcceptedQuantity;
            var rejectedQuantity = requestItem.RejectedQuantity;
            if (acceptedQuantity < 0 || rejectedQuantity < 0)
                return CustomerActionResultAppDto.Fail("Error.DeliveryAcceptance.NegativeQuantity");

            var totalQuantity = acceptedQuantity + rejectedQuantity;
            if (Math.Abs(totalQuantity - deliveryItem.Quantity) > 0.0001m)
                return CustomerActionResultAppDto.Fail("Error.DeliveryAcceptance.QuantityMismatch");

            if (rejectedQuantity > 0)
            {
                rejectedItems.Add((deliveryItem.Id, deliveryItem.ProductId, rejectedQuantity));
            }
        }

        var returnReason = dto.Acceptance?.Items?
            .Where(item => item.RejectedQuantity > 0)
            .Select(item => item.RejectReason?.Trim())
            .FirstOrDefault(reason => !string.IsNullOrWhiteSpace(reason));

        if (rejectedItems.Count > 0 && string.IsNullOrWhiteSpace(returnReason))
            return CustomerActionResultAppDto.Fail("Error.DeliveryAcceptance.RejectReasonRequired");

        try
        {
            await deliveryNoteManager.MarkReceivedByCustomerAsync(
                deliveryNote.Id,
                DateTime.UtcNow,
                dto.ReceiverName,
                dto.Note,
                new DeliveryAcceptanceDto
                {
                    AgreedCustomerCharge = dto.Acceptance?.AgreedCustomerCharge ?? 0m,
                    AgreedCustomerChargeReason = dto.Acceptance?.AgreedCustomerChargeReason,
                    Items = deliveryNote.Items.Select(item => new DeliveryAcceptanceItemDto
                    {
                        DeliveryNoteItemId = item.Id,
                        AcceptedQuantity = item.Quantity,
                        RejectedQuantity = 0m,
                        RejectReason = null
                    }).ToList()
                }).ConfigureAwait(false);
        }
        catch (NamEcommerceDomainException ex)
        {
            return CustomerActionResultAppDto.Fail(ex.ErrorCode);
        }

        if (rejectedItems.Count > 0)
        {
            await CreateReturnRequestAsync(customerId, new CreateCustomerReturnRequestAppDto
            {
                DeliveryNoteId = deliveryNote.Id,
                Reason = returnReason,
                CompensateInNextDelivery = dto.Acceptance?.CompensateInNextDelivery ?? false,
                Items = rejectedItems.Select(item => new CreateCustomerReturnRequestItemAppDto
                {
                    DeliveryNoteItemId = item.DeliveryNoteItemId,
                    ProductId = item.ProductId,
                    RequestedQuantity = item.RejectedQuantity,
                    Reason = returnReason,
                    EvidencePictures = []
                }).ToList()
            }).ConfigureAwait(false);
        }

        await customerPortalManager.CreateDeliveryFeedbackAsync(new CreateCustomerDeliveryFeedbackDto
        {
            CustomerId = customerId,
            DeliveryNoteId = deliveryNoteId,
            Message = BuildConfirmationMessage(dto)
        }).ConfigureAwait(false);

        await systemNotificationAppService
            .CreateAsync(CustomerPortalSystemNotificationComposer.DeliveryConfirmed(deliveryNote.Id, deliveryNote.Code))
            .ConfigureAwait(false);
        await UpdateCustomerLocationAsync(customerId, dto.Location, "DeliveryConfirmed").ConfigureAwait(false);
        return CustomerActionResultAppDto.Ok(
            rejectedItems.Count > 0
                ? "Msg.CustomerPortal.DeliveryConfirmedWithReturnRequest"
                : "Msg.CustomerPortal.DeliveryConfirmed");
    }

    public async Task<CustomerActionResultAppDto> CreateDeliveryFeedbackAsync(Guid customerId, CreateCustomerDeliveryFeedbackAppDto dto)
    {
        var ownsDeliveryNote = deliveryNoteReader.DataSource.Any(note => note.Id == dto.DeliveryNoteId && note.CustomerId == customerId);
        if (!ownsDeliveryNote)
            return CustomerActionResultAppDto.Fail("Error.DeliveryNoteNotFound");

        await customerPortalManager.CreateDeliveryFeedbackAsync(new CreateCustomerDeliveryFeedbackDto
        {
            CustomerId = customerId,
            DeliveryNoteId = dto.DeliveryNoteId,
            Rating = dto.Rating,
            Message = dto.Message
        }).ConfigureAwait(false);

        return CustomerActionResultAppDto.Ok("Msg.CustomerPortal.FeedbackSaved");
    }

    public async Task<IReadOnlyCollection<CustomerReturnableItemAppDto>> GetReturnableItemsAsync(Guid customerId)
    {
        var items = await customerReturnAppService.GetReturnableItemsByCustomerAsync(customerId).ConfigureAwait(false);
        return items
            .Where(item => item.OriginalQty - item.AlreadyReturnedQty > 0)
            .Select(item => new CustomerReturnableItemAppDto
            {
                ProductId = item.ProductId,
                ProductName = item.ProductName,
                Unit = item.Unit,
                DeliveredQuantity = item.OriginalQty,
                ReservedReturnQuantity = item.AlreadyReturnedQty,
                ReturnableQuantity = Math.Max(0m, item.OriginalQty - item.AlreadyReturnedQty),
                LatestUnitPrice = item.UnitPrice
            })
            .ToList();
    }

    public async Task<CustomerReturnRequestAppDto> CreateReturnRequestAsync(Guid customerId, CreateCustomerReturnRequestAppDto dto)
    {
        if (dto.Items.Count == 0)
            throw new InvalidOperationException("Error.CustomerPortal.ReturnRequest.NoItems");

        var deliveryNotes = GetDeliveredNotesForReturnRequest(customerId, dto.DeliveryNoteId);
        if (deliveryNotes.Count == 0)
            throw new InvalidOperationException("Error.CustomerPortal.ReturnRequest.NoDeliveredItems");

        var sourcesByItemId = deliveryNotes
            .SelectMany(note => note.Items.Select(item => new ReturnRequestSourceItem(note, item)))
            .ToDictionary(source => source.Item.Id);
        var requestItems = new List<CreateCustomerReturnRequestItemDto>();
        var localRequestedBySourceItemId = new Dictionary<Guid, decimal>();

        foreach (var item in dto.Items)
        {
            if (item.RequestedQuantity <= 0)
                throw new InvalidOperationException("Error.CustomerPortal.ReturnRequest.InvalidItem");

            var pictureIds = await CreateReturnEvidencePicturesAsync(item.EvidencePictures).ConfigureAwait(false);
            if (item.DeliveryNoteItemId.HasValue)
            {
                if (!sourcesByItemId.TryGetValue(item.DeliveryNoteItemId.Value, out var source) ||
                    (item.ProductId.HasValue && source.Item.ProductId != item.ProductId.Value))
                {
                    throw new InvalidOperationException("Error.CustomerPortal.ReturnRequest.InvalidItem");
                }

                var returnStates = await GetDeliveryItemReturnStatesAsync(customerId, source.DeliveryNote.Id).ConfigureAwait(false);
                localRequestedBySourceItemId.TryGetValue(source.Item.Id, out var localRequestedQuantity);
                var returnableQuantity = (returnStates.GetValueOrDefault(source.Item.Id)?.ReturnableQuantity ?? 0m)
                    - localRequestedQuantity;
                if (item.RequestedQuantity > returnableQuantity)
                    throw new InvalidOperationException("Error.CustomerPortal.ReturnRequest.QuantityExceedsReturnable");

                requestItems.Add(new CreateCustomerReturnRequestItemDto
                {
                    DeliveryNoteItemId = source.Item.Id,
                    ProductId = source.Item.ProductId,
                    ProductName = source.Item.ProductName,
                    RequestedQuantity = item.RequestedQuantity,
                    Reason = item.Reason,
                    EvidencePictureIds = pictureIds
                });
                localRequestedBySourceItemId[source.Item.Id] = localRequestedQuantity + item.RequestedQuantity;
                continue;
            }

            if (!item.ProductId.HasValue || item.ProductId.Value == Guid.Empty)
                throw new InvalidOperationException("Error.CustomerPortal.ReturnRequest.InvalidItem");

            var allocatedItems = await AllocateReturnRequestProductAsync(
                customerId,
                item.ProductId.Value,
                item.RequestedQuantity,
                item.Reason,
                pictureIds,
                deliveryNotes,
                localRequestedBySourceItemId).ConfigureAwait(false);
            requestItems.AddRange(allocatedItems);
        }

        if (requestItems.Count == 0)
            throw new InvalidOperationException("Error.CustomerPortal.ReturnRequest.NoItems");

        var representativeDeliveryNoteId = dto.DeliveryNoteId
            ?? requestItems
                .Select(item => sourcesByItemId.GetValueOrDefault(item.DeliveryNoteItemId)?.DeliveryNote.Id)
                .FirstOrDefault(id => id.HasValue)
            ?? Guid.Empty;
        if (representativeDeliveryNoteId == Guid.Empty)
            throw new InvalidOperationException("Error.CustomerPortal.ReturnRequest.NoDeliveredItems");

        var request = new CreateCustomerReturnRequestDto
        {
            CustomerId = customerId,
            DeliveryNoteId = representativeDeliveryNoteId,
            Reason = dto.Reason,
            CompensateInNextDelivery = dto.CompensateInNextDelivery,
            Items = requestItems
        };

        var created = await customerPortalManager.CreateReturnRequestAsync(request).ConfigureAwait(false);
        var deliveryNote = deliveryNotes.FirstOrDefault(note => note.Id == created.DeliveryNoteId);
        await systemNotificationAppService
            .CreateAsync(CustomerPortalSystemNotificationComposer.ReturnRequestCreated(created, deliveryNote?.Code))
            .ConfigureAwait(false);
        return new CustomerReturnRequestAppDto
        {
            Id = created.Id,
            DeliveryNoteId = created.DeliveryNoteId,
            Status = (int)created.Status,
            CreatedOnUtc = created.CreatedOnUtc,
            CompensateInNextDelivery = created.CompensateInNextDelivery
        };
    }

    public async Task<IReadOnlyCollection<CustomerReturnRequestSummaryAppDto>> GetReturnRequestsAsync(Guid customerId)
    {
        var deliveryNotes = deliveryNoteReader.DataSource
            .Where(note => note.CustomerId == customerId)
            .ToDictionary(note => note.Id);
        var requests = await customerPortalManager.GetReturnRequestsAsync(customerId).ConfigureAwait(false);

        return requests
            .OrderByDescending(request => request.CreatedOnUtc)
            .Select(request => MapReturnRequestSummary(request, deliveryNotes.GetValueOrDefault(request.DeliveryNoteId)))
            .ToList();
    }

    public async Task<CustomerReturnRequestDetailsAppDto?> GetReturnRequestDetailsAsync(Guid customerId, Guid returnRequestId)
    {
        var request = await customerPortalManager.GetReturnRequestByIdAsync(returnRequestId).ConfigureAwait(false);
        if (request is null || request.CustomerId != customerId)
            return null;

        var deliveryNote = await deliveryNoteReader.GetByIdAsync(request.DeliveryNoteId, default).ConfigureAwait(false);
        return await MapReturnRequestDetailsAsync(request, deliveryNote).ConfigureAwait(false);
    }

    public async Task<CustomerActionResultAppDto> CancelReturnRequestAsync(Guid customerId, Guid returnRequestId)
    {
        var request = await customerPortalManager.GetReturnRequestByIdAsync(returnRequestId).ConfigureAwait(false);
        if (request is null || request.CustomerId != customerId)
            return CustomerActionResultAppDto.Fail("Error.CustomerPortal.ReturnRequest.NotFound");
        if (request.Status != CustomerReturnRequestStatus.PendingReview)
            return CustomerActionResultAppDto.Fail("Error.CustomerPortal.ReturnRequest.OnlyPendingCanCancel");

        try
        {
            await customerPortalManager.CancelReturnRequestAsync(returnRequestId, DateTime.UtcNow).ConfigureAwait(false);
            return CustomerActionResultAppDto.Ok("Msg.CustomerPortal.ReturnRequest.Cancelled");
        }
        catch (InvalidOperationException)
        {
            return CustomerActionResultAppDto.Fail("Error.CustomerPortal.ReturnRequest.CannotCancelCurrentState");
        }
    }

    public async Task<CustomerDebtSummaryPortalAppDto> GetDebtSummaryAsync(Guid customerId)
    {
        var debts = await customerDebtAppService.GetDebtsByCustomerIdAsync(customerId).ConfigureAwait(false);
        if (debts is null)
            return new CustomerDebtSummaryPortalAppDto();

        return new CustomerDebtSummaryPortalAppDto
        {
            TotalDebtAmount = debts.TotalDebtAmount,
            TotalPaidAmount = debts.TotalPaidAmount,
            TotalRemainingAmount = debts.TotalRemainingAmount,
            DepositBalance = debts.DepositBalance,
            Debts = debts.Debts.Select(debt => new CustomerDebtPortalAppDto
            {
                Id = debt.Id,
                Code = debt.Code,
                OrderCode = debt.OrderCode,
                DeliveryNoteCode = debt.DeliveryNoteCode,
                TotalAmount = debt.TotalAmount,
                PaidAmount = debt.PaidAmount,
                RemainingAmount = debt.RemainingAmount,
                Status = debt.Status,
                DueDateUtc = debt.DueDateUtc
            }).ToList(),
            RecentPayments = debts.RecentPayments.Select(payment => new CustomerPaymentPortalAppDto
            {
                Id = payment.Id,
                Code = payment.Code,
                Amount = payment.Amount,
                PaymentMethod = payment.PaymentMethod,
                PaymentType = payment.PaymentType,
                PaidOnUtc = payment.PaidOnUtc
            }).ToList()
        };
    }

    public async Task<CustomerLedgerSummaryPortalAppDto> GetLedgerSummaryAsync(Guid customerId)
    {
        var summary = await customerLedgerManager.GetCustomerSummaryAsync(customerId).ConfigureAwait(false);
        var statement = await customerLedgerManager.GetStatementAsync(customerId, pageSize: 30).ConfigureAwait(false);
        return new CustomerLedgerSummaryPortalAppDto
        {
            Balance = summary?.Balance ?? 0,
            LastEntryOnUtc = summary?.LastEntryOnUtc,
            RecentEntries = statement.Items.Select(e => new CustomerLedgerStatementItemPortalAppDto(
                e.EntryId,
                (int)e.EntryType,
                e.Amount,
                e.RunningBalance,
                (int)e.ReferenceType,
                e.ReferenceId,
                e.ReferenceCode,
                e.Note,
                e.OccurredAtUtc)).ToList()
        };
    }

    public async Task<CustomerActionResultAppDto> UpdateOrderNoteAsync(Guid customerId, Guid orderId, string? note)
    {
        var order = await orderAppService.GetOrderByIdAsync(orderId).ConfigureAwait(false);
        if (order is null || order.CustomerId != customerId)
            return CustomerActionResultAppDto.Fail("Error.CustomerPortal.Order.NotFound");

        if (!order.CanUpdateInfo)
            return CustomerActionResultAppDto.Fail("Error.CustomerPortal.Order.CannotUpdate");

        var result = await orderAppService.UpdateOrderAsync(new UpdateOrderAppDto(orderId)
        {
            ExpectedShippingDateUtc = order.ExpectedShippingDateUtc,
            OrderDiscount = order.OrderDiscount,
            Note = note
        }).ConfigureAwait(false);

        return result.Success
            ? CustomerActionResultAppDto.Ok()
            : CustomerActionResultAppDto.Fail(result.ErrorMessage);
    }

    private static CustomerOrderSummaryAppDto MapOrderSummary(Order order)
        => new()
        {
            Id = order.Id,
            Code = order.Code,
            Status = (int)order.OrderStatus,
            TotalAmount = order.OrderTotal,
            CreatedOnUtc = order.CreatedOnUtc,
            ExpectedShippingDateUtc = order.ExpectedShippingDateUtc
        };

    private static CustomerOrderRequestSummaryAppDto MapOrderRequestSummary(CustomerOrderRequestDto request)
    {
        var exposePrice = ShouldExposeOrderRequestPrice(request);
        return new CustomerOrderRequestSummaryAppDto
        {
            Id = request.Id,
            Code = request.Code,
            Status = (int)request.Status,
            TotalAmount = exposePrice ? request.Items.Sum(item => item.SubTotal) : null,
            CreatedOnUtc = request.CreatedOnUtc,
            ExpectedShippingDateUtc = request.ExpectedShippingDateUtc,
            ReviewedOnUtc = request.ReviewedOnUtc,
            ConvertedOrderId = request.ConvertedOrderId,
            CanConfirm = request.Status == CustomerOrderRequestStatus.Approved && IsOrderRequestPriced(request)
        };
    }

    private static CustomerOrderRequestDetailsAppDto MapOrderRequestDetails(CustomerOrderRequestDto request)
    {
        var exposePrice = ShouldExposeOrderRequestPrice(request);
        return new CustomerOrderRequestDetailsAppDto
        {
            Id = request.Id,
            Code = request.Code,
            Status = (int)request.Status,
            TotalAmount = exposePrice ? request.Items.Sum(item => item.SubTotal) : null,
            CreatedOnUtc = request.CreatedOnUtc,
            ExpectedShippingDateUtc = request.ExpectedShippingDateUtc,
            ReviewedOnUtc = request.ReviewedOnUtc,
            ConvertedOrderId = request.ConvertedOrderId,
            CanConfirm = request.Status == CustomerOrderRequestStatus.Approved && IsOrderRequestPriced(request),
            ShippingAddress = request.ShippingAddress,
            Note = request.Note,
            AdminNote = request.AdminNote,
            Items = request.Items.Select(item => MapOrderRequestItem(item, exposePrice)).ToList()
        };
    }

    private static CustomerOrderRequestItemAppDto MapOrderRequestItem(CustomerOrderRequestItemDto item, bool exposePrice)
        => new()
        {
            Id = item.Id,
            ProductId = item.ProductId,
            ProductName = item.ProductName,
            Quantity = item.Quantity,
            UnitPrice = exposePrice ? item.UnitPriceSnapshot : null,
            SubTotal = exposePrice ? item.SubTotal : null,
            IsPriced = item.UnitPriceSnapshot > 0
        };

    private static CustomerDeliveryNoteSummaryAppDto MapDeliveryNoteSummary(DeliveryNote deliveryNote)
        => new()
        {
            Id = deliveryNote.Id,
            Code = deliveryNote.Code,
            OrderCode = deliveryNote.OrderCode,
            Status = (int)deliveryNote.Status,
            DeliveryConfirmationStatus = (int)deliveryNote.DeliveryConfirmationStatus,
            CreatedOnUtc = deliveryNote.CreatedOnUtc,
            DeliveredOnUtc = deliveryNote.DeliveredOnUtc
        };

    private static CustomerReturnRequestSummaryAppDto MapReturnRequestSummary(CustomerReturnRequestDto request, DeliveryNote? deliveryNote)
        => new()
        {
            Id = request.Id,
            DeliveryNoteId = request.DeliveryNoteId,
            DeliveryNoteCode = deliveryNote?.Code,
            Status = (int)request.Status,
            Reason = request.Reason,
            CompensateInNextDelivery = request.CompensateInNextDelivery,
            AdminNote = request.AdminNote,
            CreatedOnUtc = request.CreatedOnUtc,
            ReviewedOnUtc = request.ReviewedOnUtc,
            ConvertedCustomerReturnId = request.ConvertedCustomerReturnId,
            TotalRequestedQuantity = request.Items.Sum(item => item.RequestedQuantity),
            ItemCount = request.Items.Count
        };

    private async Task<CustomerReturnRequestDetailsAppDto> MapReturnRequestDetailsAsync(CustomerReturnRequestDto request, DeliveryNote? deliveryNote)
    {
        var details = new CustomerReturnRequestDetailsAppDto
        {
            Id = request.Id,
            DeliveryNoteId = request.DeliveryNoteId,
            DeliveryNoteCode = deliveryNote?.Code,
            Status = (int)request.Status,
            Reason = request.Reason,
            CompensateInNextDelivery = request.CompensateInNextDelivery,
            AdminNote = request.AdminNote,
            CreatedOnUtc = request.CreatedOnUtc,
            ReviewedOnUtc = request.ReviewedOnUtc,
            ConvertedCustomerReturnId = request.ConvertedCustomerReturnId,
            TotalRequestedQuantity = request.Items.Sum(item => item.RequestedQuantity),
            ItemCount = request.Items.Count
        };

        foreach (var item in request.Items)
        {
            var mappedItem = new CustomerReturnRequestItemAppDto
            {
                Id = item.Id,
                DeliveryNoteItemId = item.DeliveryNoteItemId,
                ProductId = item.ProductId,
                ProductName = item.ProductName,
                RequestedQuantity = item.RequestedQuantity,
                Reason = item.Reason
            };

            foreach (var picture in item.EvidencePictures)
            {
                var pictureDto = await pictureAppService.GetBase64PictureByIdAsync(picture.PictureId).ConfigureAwait(false);
                mappedItem.EvidencePictures.Add(new CustomerReturnRequestEvidencePictureAppDto
                {
                    PictureId = picture.PictureId,
                    PictureUrl = pictureDto?.Base64Value,
                    FileName = pictureDto?.FileName
                });
            }

            details.Items.Add(mappedItem);
        }

        return details;
    }

    private static string BuildConfirmationMessage(ConfirmCustomerDeliveryNoteAppDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.ReceiverName))
            return dto.Note ?? "Customer confirmed delivery.";

        return string.IsNullOrWhiteSpace(dto.Note)
            ? $"Customer confirmed delivery. Receiver: {dto.ReceiverName}"
            : $"Customer confirmed delivery. Receiver: {dto.ReceiverName}. Note: {dto.Note}";
    }

    private Task UpdateCustomerLocationAsync(Guid customerId, CustomerPortalLocationAppDto? location, string source)
    {
        if (location is null)
            return Task.CompletedTask;

        return securityManager.UpdateLastKnownLocationAsync(customerId, new UpdateCustomerPortalLocationDto
        {
            Latitude = location.Latitude,
            Longitude = location.Longitude,
            AccuracyMeters = location.AccuracyMeters,
            Source = source,
            CapturedOnUtc = DateTime.UtcNow
        });
    }

    private List<DeliveryNote> GetDeliveredNotesForReturnRequest(Guid customerId, Guid? deliveryNoteId)
    {
        if (deliveryNoteId.HasValue)
        {
            var deliveryNote = deliveryNoteReader.DataSource.FirstOrDefault(note => note.Id == deliveryNoteId.Value && note.CustomerId == customerId);
            if (deliveryNote is null)
                throw new InvalidOperationException("Error.DeliveryNoteNotFound");
            if (deliveryNote.Status != DeliveryNoteStatus.Delivered)
                throw new InvalidOperationException("Error.CustomerPortal.ReturnRequest.DeliveryNoteNotDelivered");
            if (deliveryNote.SourceType != DeliveryNoteSourceType.ToCustomer &&
                deliveryNote.SourceType != DeliveryNoteSourceType.DirectShipToCustomer)
                throw new InvalidOperationException("Error.CustomerPortal.ReturnRequest.InvalidDeliveryNoteForReturn");

            return [deliveryNote];
        }

        return deliveryNoteReader.DataSource
            .Where(note => note.CustomerId == customerId
                && note.Status == DeliveryNoteStatus.Delivered
                && (note.SourceType == DeliveryNoteSourceType.ToCustomer ||
                    note.SourceType == DeliveryNoteSourceType.DirectShipToCustomer))
            .OrderBy(note => note.DeliveredOnUtc ?? note.CreatedOnUtc)
            .ToList();
    }

    private async Task<IList<CreateCustomerReturnRequestItemDto>> AllocateReturnRequestProductAsync(
        Guid customerId,
        Guid productId,
        decimal requestedQuantity,
        string? reason,
        IList<Guid> evidencePictureIds,
        IReadOnlyCollection<DeliveryNote> deliveryNotes,
        IDictionary<Guid, decimal> localRequestedBySourceItemId)
    {
        var sources = deliveryNotes
            .SelectMany(note => note.Items
                .Where(item => item.ProductId == productId)
                .Select(item => new ReturnRequestSourceItem(note, item)))
            .OrderBy(source => source.DeliveryNote.DeliveredOnUtc ?? source.DeliveryNote.CreatedOnUtc)
            .ToList();

        if (sources.Count == 0)
            throw new InvalidOperationException("Error.CustomerPortal.ReturnRequest.ProductNotDelivered");

        var remainingQuantity = requestedQuantity;
        var result = new List<CreateCustomerReturnRequestItemDto>();
        foreach (var source in sources)
        {
            var returnStates = await GetDeliveryItemReturnStatesAsync(customerId, source.DeliveryNote.Id).ConfigureAwait(false);
            localRequestedBySourceItemId.TryGetValue(source.Item.Id, out var localRequestedQuantity);
            var returnableQuantity = (returnStates.GetValueOrDefault(source.Item.Id)?.ReturnableQuantity ?? 0m)
                - localRequestedQuantity;
            if (returnableQuantity <= 0)
                continue;

            var allocatedQuantity = Math.Min(remainingQuantity, returnableQuantity);
            result.Add(new CreateCustomerReturnRequestItemDto
            {
                DeliveryNoteItemId = source.Item.Id,
                ProductId = source.Item.ProductId,
                ProductName = source.Item.ProductName,
                RequestedQuantity = allocatedQuantity,
                Reason = reason,
                EvidencePictureIds = evidencePictureIds
            });

            remainingQuantity -= allocatedQuantity;
            localRequestedBySourceItemId[source.Item.Id] = localRequestedQuantity + allocatedQuantity;
            if (remainingQuantity <= 0)
                break;
        }

        if (remainingQuantity > 0)
            throw new InvalidOperationException("Error.CustomerPortal.ReturnRequest.QuantityExceedsReturnable");

        return result;
    }

    private async Task<IReadOnlyDictionary<Guid, DeliveryItemReturnState>> GetDeliveryItemReturnStatesAsync(Guid customerId, Guid deliveryNoteId)
    {
        var returnableItems = await customerReturnAppService
            .GetDeliveryNoteItemsForReturnAsync(deliveryNoteId)
            .ConfigureAwait(false);
        var pendingPortalQuantitiesByItem = (await customerPortalManager.GetReturnRequestsAsync(customerId).ConfigureAwait(false))
            .Where(request => request.DeliveryNoteId == deliveryNoteId
                && (request.Status == CustomerReturnRequestStatus.PendingReview || request.Status == CustomerReturnRequestStatus.Accepted))
            .SelectMany(request => request.Items)
            .GroupBy(item => item.DeliveryNoteItemId)
            .ToDictionary(group => group.Key, group => group.Sum(item => item.RequestedQuantity));

        return returnableItems
            .Where(item => item.SourceItemId.HasValue)
            .ToDictionary(
                item => item.SourceItemId!.Value,
                item =>
                {
                    var totalReservedQuantity = Math.Max(0m, item.AlreadyReturnedQty);
                    var pendingPortalQuantity = Math.Max(0m, pendingPortalQuantitiesByItem.GetValueOrDefault(item.SourceItemId!.Value));
                    var reservedReturnQuantity = Math.Max(0m, totalReservedQuantity - pendingPortalQuantity);
                    var returnableQuantity = Math.Max(0m, item.OriginalQty - totalReservedQuantity);
                    return new DeliveryItemReturnState(reservedReturnQuantity, pendingPortalQuantity, returnableQuantity);
                });
    }

    private async Task<IList<Guid>> CreateReturnEvidencePicturesAsync(ICollection<CreateCustomerReturnRequestPictureAppDto> pictures)
    {
        if (pictures.Count > MaxReturnEvidencePicturesPerItem)
            throw new InvalidOperationException("Error.CustomerPortal.ReturnEvidence.TooManyPictures");

        var pictureIds = new List<Guid>();
        foreach (var picture in pictures)
        {
            if (!AllowedReturnEvidenceMimeTypes.Contains(picture.MimeType))
                throw new InvalidOperationException("Error.CustomerPortal.ReturnEvidence.InvalidMimeType");

            var bytes = DecodeBase64Payload(picture.Base64Data);
            if (bytes.Length == 0 || bytes.Length > MaxReturnEvidencePictureBytes)
                throw new InvalidOperationException("Error.CustomerPortal.ReturnEvidence.InvalidSize");

            pictureIds.Add(await pictureAppService.CreatePictureAsync(new CreatePictureAppDto
            {
                Data = bytes,
                MimeType = picture.MimeType,
                FileName = string.IsNullOrWhiteSpace(picture.FileName) ? "return-evidence" : picture.FileName.Trim(),
                Extension = ResolvePictureExtension(picture.FileName, picture.MimeType)
            }).ConfigureAwait(false));
        }

        return pictureIds;
    }

    private static byte[] DecodeBase64Payload(string base64Data)
    {
        if (string.IsNullOrWhiteSpace(base64Data))
            return [];

        var commaIndex = base64Data.IndexOf(',');
        var payload = commaIndex >= 0 ? base64Data[(commaIndex + 1)..] : base64Data;
        try
        {
            return Convert.FromBase64String(payload);
        }
        catch (FormatException ex)
        {
            throw new InvalidOperationException("Error.CustomerPortal.ReturnEvidence.InvalidBase64", ex);
        }
    }

    private static string ResolvePictureExtension(string fileName, string mimeType)
    {
        var extension = Path.GetExtension(fileName);
        if (!string.IsNullOrWhiteSpace(extension))
            return extension.TrimStart('.').ToLowerInvariant();

        return mimeType.ToLowerInvariant() switch
        {
            "image/jpeg" => "jpg",
            "image/png" => "png",
            "image/webp" => "webp",
            _ => "img"
        };
    }

    private Dictionary<Guid, decimal> GetLatestPurchasedPrices(Guid customerId, IReadOnlyCollection<Guid> productIds)
    {
        if (productIds.Count == 0)
            return [];

        return orderReader.DataSource
            .Where(order => order.CustomerId == customerId && order.OrderStatus != OrderStatus.Cancelled)
            .SelectMany(order => order.OrderItems.Select(item => new
            {
                item.ProductId,
                item.UnitPrice,
                order.CreatedOnUtc
            }))
            .Where(item => productIds.Contains(item.ProductId))
            .OrderByDescending(item => item.CreatedOnUtc)
            .ToList()
            .GroupBy(item => item.ProductId)
            .ToDictionary(group => group.Key, group => group.First().UnitPrice);
    }

    private static bool ShouldExposeOrderRequestPrice(CustomerOrderRequestDto request)
        => request.Status is CustomerOrderRequestStatus.Approved or CustomerOrderRequestStatus.ConvertedToOrder;

    private static bool IsOrderRequestPriced(CustomerOrderRequestDto request)
        => request.Items.Count > 0 && request.Items.All(item => item.UnitPriceSnapshot > 0);

    private static string BuildConvertedOrderNote(CustomerOrderRequestDto request, string actorNote)
    {
        var note = $"Tạo từ yêu cầu Customer Portal {request.Code}. {actorNote}";
        if (!string.IsNullOrWhiteSpace(request.Note))
            note += $" Ghi chú khách: {request.Note}";
        if (!string.IsNullOrWhiteSpace(request.AdminNote))
            note += $" Ghi chú duyệt: {request.AdminNote}";

        return note;
    }

    private static string? NullIfWhiteSpace(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private sealed record CustomerPortalProductListItem(
        Guid Id,
        string Name,
        Guid? CategoryId,
        Guid? PictureId);

    private sealed record ReturnRequestSourceItem(
        DeliveryNote DeliveryNote,
        DeliveryNoteItem Item);

    private sealed record DeliveryItemReturnState(
        decimal ReservedReturnQuantity,
        decimal PendingPortalReturnQuantity,
        decimal ReturnableQuantity);
}
