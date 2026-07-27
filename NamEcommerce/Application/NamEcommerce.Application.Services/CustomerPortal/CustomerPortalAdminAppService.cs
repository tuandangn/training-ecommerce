using System.Text.Json;
using NamEcommerce.Application.Contracts.CustomerPortal;
using NamEcommerce.Application.Contracts.Debts;
using NamEcommerce.Application.Contracts.Dtos.CustomerPortal;
using NamEcommerce.Application.Contracts.Dtos.Debts;
using NamEcommerce.Application.Contracts.Dtos.Orders;
using NamEcommerce.Application.Contracts.Dtos.Returns;
using NamEcommerce.Application.Contracts.Media;
using NamEcommerce.Application.Contracts.Orders;
using NamEcommerce.Application.Contracts.Returns;
using NamEcommerce.Domain.Entities.CustomerPortal;
using NamEcommerce.Domain.Entities.Customers;
using NamEcommerce.Domain.Entities.DeliveryNotes;
using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Domain.Shared.Dtos.CustomerPortal;
using NamEcommerce.Domain.Shared.Enums.CustomerPortal;
using NamEcommerce.Domain.Shared.Enums.Debts;
using NamEcommerce.Domain.Shared.Enums.DeliveryNotes;
using NamEcommerce.Domain.Shared.Enums.Orders;
using NamEcommerce.Domain.Shared.Services.CustomerPortal;
using NamEcommerce.Domain.Shared.Services.Security;
using NamEcommerce.Domain.Shared.Services.Users;

namespace NamEcommerce.Application.Services.CustomerPortal;

public sealed class CustomerPortalAdminAppService(
    ICustomerPortalSecurityManager securityManager,
    ICustomerPortalManager customerPortalManager,
    ISecurityService securityService,
    ICustomerDebtAppService customerDebtAppService,
    IOrderAppService orderAppService,
    IEnumerable<ICustomerPortalNotificationSender> notificationSenders,
    IPictureAppService pictureAppService,
    ICustomerReturnAppService customerReturnAppService,
    ICurrentUserAccessor currentUserAccessor,
    IEntityDataReader<CustomerPortalAccount> accountReader,
    IEntityDataReader<CustomerSecurityEvent> securityEventReader,
    IEntityDataReader<CustomerOrderRequest> orderRequestReader,
    IEntityDataReader<CustomerReturnRequest> returnRequestReader,
    IEntityDataReader<CustomerReturnRequestItemPicture> returnRequestItemPictureReader,
    IEntityDataReader<CustomerPaymentIntent> paymentIntentReader,
    IEntityDataReader<Customer> customerReader,
    IEntityDataReader<DeliveryNote> deliveryNoteReader) : ICustomerPortalAdminAppService
{
    public async Task<CustomerPortalAdminOverviewAppDto> GetOverviewAsync()
        => new()
        {
            Settings = await GetSettingsAsync().ConfigureAwait(false),
            Accounts = (await GetAccountsAsync().ConfigureAwait(false)).Take(10).ToList(),
            RecentSecurityEvents = (await GetSecurityEventsAsync(take: 10).ConfigureAwait(false)).ToList(),
            PendingOrderRequests = (await GetOrderRequestsAsync((int)CustomerOrderRequestStatus.PendingApproval).ConfigureAwait(false)).Take(10).ToList(),
            PendingReturnRequests = (await GetReturnRequestsAsync((int)CustomerReturnRequestStatus.PendingReview).ConfigureAwait(false)).Take(10).ToList(),
            PendingPaymentIntents = (await GetPaymentIntentsAsync((int)CustomerPaymentIntentStatus.SucceededPendingReconciliation).ConfigureAwait(false)).Take(10).ToList()
        };

    public async Task<CustomerPortalSettingsAdminAppDto> GetSettingsAsync()
        => MapSettings(await securityManager.GetSettingsAsync().ConfigureAwait(false));

    public async Task<CustomerActionResultAppDto> UpdateSettingsAsync(UpdateCustomerPortalSettingsAdminAppDto dto)
    {
        var currentUser = await currentUserAccessor.GetCurrentUserAsync().ConfigureAwait(false);
        await securityManager.UpdateSettingsAsync(dto.OtpEnabled, currentUser?.Id, DateTime.UtcNow).ConfigureAwait(false);

        return CustomerActionResultAppDto.Ok(dto.OtpEnabled
            ? "Đã bật xác thực OTP cho Customer Portal."
            : "Đã tắt xác thực OTP cho Customer Portal.");
    }

    public Task<IReadOnlyCollection<CustomerPortalAccountAdminAppDto>> GetAccountsAsync()
    {
        var customers = customerReader.DataSource.ToDictionary(customer => customer.Id);
        var accounts = accountReader.DataSource
            .OrderByDescending(account => account.UpdatedOnUtc ?? account.CreatedOnUtc)
            .Select(account => MapAccount(account, customers.GetValueOrDefault(account.CustomerId)))
            .ToList();

        return Task.FromResult<IReadOnlyCollection<CustomerPortalAccountAdminAppDto>>(accounts);
    }

    public async Task<CustomerPortalAccountAdminAppDto?> GetAccountAsync(Guid customerId)
    {
        var account = await securityManager.GetAccountByCustomerIdAsync(customerId).ConfigureAwait(false);
        if (account is null)
            return null;

        var customer = await customerReader.GetByIdAsync(customerId).ConfigureAwait(false);
        return MapAccount(account, customer);
    }

    public async Task<CustomerActionResultAppDto> BlockAccountAsync(Guid customerId)
    {
        var currentUser = await currentUserAccessor.GetCurrentUserAsync().ConfigureAwait(false);
        await securityManager.BlockAccountAsync(customerId).ConfigureAwait(false);
        await securityManager.RecordSecurityEventAsync(new CreateCustomerSecurityEventDto
        {
            CustomerId = customerId,
            EventType = "AdminBlockedAccount",
            Outcome = CustomerPortalSecurityEventOutcome.Succeeded,
            MetadataJson = currentUser is null ? null : $"{{\"adminUserId\":\"{currentUser.Id}\"}}"
        }).ConfigureAwait(false);

        return CustomerActionResultAppDto.Ok("Đã khóa truy cập portal của khách hàng.");
    }

    public async Task<CustomerActionResultAppDto> UnblockAccountAsync(Guid customerId)
    {
        var currentUser = await currentUserAccessor.GetCurrentUserAsync().ConfigureAwait(false);
        await securityManager.UnblockAccountAsync(customerId).ConfigureAwait(false);
        await securityManager.RecordSecurityEventAsync(new CreateCustomerSecurityEventDto
        {
            CustomerId = customerId,
            EventType = "AdminUnblockedAccount",
            Outcome = CustomerPortalSecurityEventOutcome.Succeeded,
            MetadataJson = currentUser is null ? null : $"{{\"adminUserId\":\"{currentUser.Id}\"}}"
        }).ConfigureAwait(false);

        return CustomerActionResultAppDto.Ok("Đã mở khóa truy cập portal của khách hàng.");
    }

    public async Task<CustomerActionResultAppDto> ResetAccountPasswordAsync(Guid customerId, string password)
    {
        if (customerId == Guid.Empty)
            return CustomerActionResultAppDto.Fail("Không tìm thấy khách hàng.");
        if (string.IsNullOrWhiteSpace(password) || password.Length < 8)
            return CustomerActionResultAppDto.Fail("Mật khẩu mới cần tối thiểu 8 ký tự.");

        var customer = await customerReader.GetByIdAsync(customerId).ConfigureAwait(false);
        if (customer is null)
            return CustomerActionResultAppDto.Fail("Không tìm thấy khách hàng.");

        var currentUser = await currentUserAccessor.GetCurrentUserAsync().ConfigureAwait(false);
        var account = await securityManager.GetAccountByCustomerIdAsync(customerId).ConfigureAwait(false);
        var hash = await securityService.HashPasswordAsync(password).ConfigureAwait(false);

        await securityManager.SetPasswordAsync(customerId, hash.PasswordHash, hash.PasswordSalt, markLoginSucceeded: false).ConfigureAwait(false);
        await securityManager.RecordSecurityEventAsync(new CreateCustomerSecurityEventDto
        {
            CustomerId = customerId,
            EventType = "AdminResetPassword",
            Outcome = CustomerPortalSecurityEventOutcome.Succeeded,
            MetadataJson = JsonSerializer.Serialize(new
            {
                adminUserId = currentUser?.Id,
                hadPassword = !string.IsNullOrWhiteSpace(account?.PasswordHash)
            })
        }).ConfigureAwait(false);

        return account?.Status == CustomerPortalAccountStatus.Blocked
            ? CustomerActionResultAppDto.Ok("Đã đặt lại mật khẩu. Tài khoản đang bị khóa, hãy mở khóa trước khi khách đăng nhập.")
            : CustomerActionResultAppDto.Ok("Đã đặt lại mật khẩu portal cho khách hàng.");
    }

    public Task<IReadOnlyCollection<CustomerPortalSecurityEventAdminAppDto>> GetSecurityEventsAsync(Guid? customerId = null, int take = 100)
    {
        take = Math.Clamp(take, 1, 500);

        var customers = customerReader.DataSource.ToDictionary(customer => customer.Id);
        var deliveryNotes = deliveryNoteReader.DataSource.ToDictionary(note => note.Id);
        var query = securityEventReader.DataSource.AsQueryable();

        if (customerId.HasValue)
            query = query.Where(securityEvent => securityEvent.CustomerId == customerId.Value);

        var events = query
            .OrderByDescending(securityEvent => securityEvent.CreatedOnUtc)
            .Take(take)
            .ToList()
            .Select(securityEvent => MapSecurityEvent(
                securityEvent,
                securityEvent.CustomerId.HasValue ? customers.GetValueOrDefault(securityEvent.CustomerId.Value) : null,
                securityEvent.DeliveryNoteId.HasValue ? deliveryNotes.GetValueOrDefault(securityEvent.DeliveryNoteId.Value) : null))
            .ToList();

        return Task.FromResult<IReadOnlyCollection<CustomerPortalSecurityEventAdminAppDto>>(events);
    }

    public Task<IReadOnlyCollection<CustomerPortalOrderRequestAdminAppDto>> GetOrderRequestsAsync(int? status = null)
    {
        var customers = customerReader.DataSource.ToDictionary(customer => customer.Id);
        var query = orderRequestReader.DataSource.AsQueryable();

        if (status.HasValue)
            query = query.Where(request => request.Status == (CustomerOrderRequestStatus)status.Value);

        var requests = query
            .OrderByDescending(request => request.CreatedOnUtc)
            .ToList()
            .Select(request => MapOrderRequest(request, customers.GetValueOrDefault(request.CustomerId)))
            .ToList();

        return Task.FromResult<IReadOnlyCollection<CustomerPortalOrderRequestAdminAppDto>>(requests);
    }

    public async Task<CustomerPortalOrderRequestAdminAppDto?> GetOrderRequestAsync(Guid id)
    {
        var request = await orderRequestReader.GetByIdAsync(id).ConfigureAwait(false);
        if (request is null)
            return null;

        var customer = await customerReader.GetByIdAsync(request.CustomerId).ConfigureAwait(false);
        return MapOrderRequest(request, customer);
    }

    public async Task<CustomerActionResultAppDto> ApproveOrderRequestAsync(Guid id, IReadOnlyDictionary<Guid, decimal> itemPrices, string? adminNote)
    {
        var currentUser = await currentUserAccessor.GetCurrentUserAsync().ConfigureAwait(false);
        if (currentUser is null)
            return CustomerActionResultAppDto.Fail("Không xác định được người duyệt.");

        var request = await customerPortalManager.GetOrderRequestByIdAsync(id).ConfigureAwait(false);
        if (request is null)
            return CustomerActionResultAppDto.Fail("Không tìm thấy yêu cầu đặt hàng.");
        if (request.Status is not CustomerOrderRequestStatus.PendingApproval)
            return CustomerActionResultAppDto.Fail("Chỉ duyệt được yêu cầu đang chờ duyệt.");
        if (request.Items.Any(item => !itemPrices.TryGetValue(item.Id, out var unitPrice) || unitPrice <= 0))
            return CustomerActionResultAppDto.Fail("Vui lòng nhập đơn giá lớn hơn 0 cho tất cả hàng hóa trước khi duyệt.");

        await customerPortalManager.ApproveOrderRequestAsync(id, currentUser.Id, itemPrices, adminNote, DateTime.UtcNow).ConfigureAwait(false);
        var approvedRequest = await customerPortalManager.GetOrderRequestByIdAsync(id).ConfigureAwait(false);
        await NotifyOrderRequestApprovedAsync(approvedRequest ?? request).ConfigureAwait(false);

        return CustomerActionResultAppDto.Ok("Đã duyệt và mock thông báo cho khách hàng.");
    }

    public async Task<CustomerActionResultAppDto> RejectOrderRequestAsync(Guid id, string? adminNote)
    {
        var currentUser = await currentUserAccessor.GetCurrentUserAsync().ConfigureAwait(false);
        if (currentUser is null)
            return CustomerActionResultAppDto.Fail("Không xác định được người duyệt.");

        await customerPortalManager.RejectOrderRequestAsync(id, currentUser.Id, adminNote, DateTime.UtcNow).ConfigureAwait(false);
        return CustomerActionResultAppDto.Ok("Đã từ chối yêu cầu đặt hàng.");
    }

    public async Task<CustomerPortalConversionResultAppDto> ConvertOrderRequestAsync(Guid id)
    {
        var request = await customerPortalManager.GetOrderRequestByIdAsync(id).ConfigureAwait(false);
        if (request is null)
            return CustomerPortalConversionResultAppDto.Fail("Không tìm thấy yêu cầu đặt hàng.");

        if (request.Status is not CustomerOrderRequestStatus.Approved)
            return CustomerPortalConversionResultAppDto.Fail("Chỉ chuyển được yêu cầu đã duyệt.");
        if (request.Items.Any(item => item.UnitPriceSnapshot <= 0))
            return CustomerPortalConversionResultAppDto.Fail("Yêu cầu đặt hàng chưa được báo giá đầy đủ.");

        var customer = await customerReader.GetByIdAsync(request.CustomerId).ConfigureAwait(false);
        var createDto = new CreateOrderAppDto
        {
            CustomerId = request.CustomerId,
            ExpectedShippingDateUtc = request.ExpectedShippingDateUtc,
            ShippingAddress = request.ShippingAddress,
            ShippingPhoneNumber = customer?.PhoneNumber,
            Note = BuildConvertedOrderNote(request)
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
            return CustomerPortalConversionResultAppDto.Fail(result.ErrorMessage ?? "Không tạo được đơn hàng.");

        await customerPortalManager.MarkOrderRequestConvertedAsync(request.Id, result.CreatedId.Value, DateTime.UtcNow).ConfigureAwait(false);
        return CustomerPortalConversionResultAppDto.Ok(result.CreatedId.Value, "Đã tạo đơn hàng nội bộ từ yêu cầu portal.");
    }

    public Task<IReadOnlyCollection<CustomerPortalReturnRequestAdminAppDto>> GetReturnRequestsAsync(int? status = null)
    {
        var customers = customerReader.DataSource.ToDictionary(customer => customer.Id);
        var deliveryNotes = deliveryNoteReader.DataSource.ToDictionary(note => note.Id);
        var query = returnRequestReader.DataSource.AsQueryable();

        if (status.HasValue)
            query = query.Where(request => request.Status == (CustomerReturnRequestStatus)status.Value);

        var requests = query
            .OrderByDescending(request => request.CreatedOnUtc)
            .ToList()
            .Select(request => MapReturnRequest(request, customers.GetValueOrDefault(request.CustomerId), deliveryNotes.GetValueOrDefault(request.DeliveryNoteId)))
            .ToList();

        return Task.FromResult<IReadOnlyCollection<CustomerPortalReturnRequestAdminAppDto>>(requests);
    }

    public async Task<CustomerPortalReturnRequestAdminAppDto?> GetReturnRequestAsync(Guid id)
    {
        var request = await returnRequestReader.GetByIdAsync(id).ConfigureAwait(false);
        if (request is null)
            return null;

        var customer = await customerReader.GetByIdAsync(request.CustomerId).ConfigureAwait(false);
        var deliveryNote = await deliveryNoteReader.GetByIdAsync(request.DeliveryNoteId).ConfigureAwait(false);
        var mapped = MapReturnRequest(request, customer, deliveryNote);
        await PopulateReturnEvidencePicturesAsync(mapped).ConfigureAwait(false);
        return mapped;
    }

    public async Task<CustomerActionResultAppDto> AcceptReturnRequestAsync(Guid id, string? adminNote)
    {
        var currentUser = await currentUserAccessor.GetCurrentUserAsync().ConfigureAwait(false);
        if (currentUser is null)
            return CustomerActionResultAppDto.Fail("Không xác định được người duyệt.");

        await customerPortalManager.AcceptReturnRequestAsync(id, currentUser.Id, adminNote, DateTime.UtcNow).ConfigureAwait(false);
        return CustomerActionResultAppDto.Ok("Đã chấp nhận yêu cầu trả hàng.");
    }

    public async Task<CustomerActionResultAppDto> RejectReturnRequestAsync(Guid id, string? adminNote)
    {
        var currentUser = await currentUserAccessor.GetCurrentUserAsync().ConfigureAwait(false);
        if (currentUser is null)
            return CustomerActionResultAppDto.Fail("Không xác định được người duyệt.");

        await customerPortalManager.RejectReturnRequestAsync(id, currentUser.Id, adminNote, DateTime.UtcNow).ConfigureAwait(false);
        return CustomerActionResultAppDto.Ok("Đã từ chối yêu cầu trả hàng.");
    }

    public async Task<CustomerPortalConversionResultAppDto> ConvertReturnRequestAsync(
        Guid id,
        Guid warehouseId,
        IReadOnlyCollection<CustomerPortalReturnConversionItemAppDto> items,
        decimal additionalCost,
        string? adminNote)
    {
        if (warehouseId == Guid.Empty)
            return CustomerPortalConversionResultAppDto.Fail("Vui lòng chọn kho nhận hàng trả.");
        if (additionalCost < 0)
            return CustomerPortalConversionResultAppDto.Fail("Chi phí phát sinh không được âm.");
        if (items.Count == 0)
            return CustomerPortalConversionResultAppDto.Fail("Vui lòng nhập số lượng thực nhận cho hàng trả.");

        var request = await customerPortalManager.GetReturnRequestByIdAsync(id).ConfigureAwait(false);
        if (request is null)
            return CustomerPortalConversionResultAppDto.Fail("Không tìm thấy yêu cầu trả hàng.");

        if (request.Status is not CustomerReturnRequestStatus.Accepted)
            return CustomerPortalConversionResultAppDto.Fail("Chỉ chuyển được yêu cầu đã chấp nhận.");

        var deliveryNote = await deliveryNoteReader.GetByIdAsync(request.DeliveryNoteId).ConfigureAwait(false);
        if (deliveryNote is null)
            return CustomerPortalConversionResultAppDto.Fail("Không tìm thấy phiếu giao hàng tham chiếu.");
        if (deliveryNote.CustomerId != request.CustomerId)
            return CustomerPortalConversionResultAppDto.Fail("Phiếu giao hàng tham chiếu không thuộc khách hàng này.");

        var deliveryItems = deliveryNoteReader.DataSource
            .Where(note => note.CustomerId == request.CustomerId && note.Status == DeliveryNoteStatus.Delivered)
            .SelectMany(note => note.Items.Select(item => new { note, item }))
            .ToDictionary(source => source.item.Id);
        if (request.Items.Any(item => !deliveryItems.ContainsKey(item.DeliveryNoteItemId)))
            return CustomerPortalConversionResultAppDto.Fail("Dữ liệu dòng hàng trả không còn khớp với hàng đã giao.");

        var conversionByRequestItemId = items
            .GroupBy(item => item.RequestItemId)
            .ToDictionary(group => group.Key, group => group.Last());
        if (request.Items.Any(item => !conversionByRequestItemId.ContainsKey(item.Id)))
            return CustomerPortalConversionResultAppDto.Fail("Vui lòng nhập đủ số lượng thực nhận cho hàng trả.");
        if (conversionByRequestItemId.Values.Any(item => item.AcceptedQuantity < 0 || item.ReturnUnitPrice < 0))
            return CustomerPortalConversionResultAppDto.Fail("Số lượng thực nhận và đơn giá hoàn không được âm.");

        var returnItems = new List<CreateCustomerReturnItemAppDto>();
        foreach (var item in request.Items)
        {
            var conversion = conversionByRequestItemId[item.Id];
            if (conversion.AcceptedQuantity > item.RequestedQuantity)
                return CustomerPortalConversionResultAppDto.Fail("Số lượng thực nhận không được vượt quá số lượng khách yêu cầu.");
            if (conversion.AcceptedQuantity <= 0)
                continue;

            var deliveryItem = deliveryItems.GetValueOrDefault(item.DeliveryNoteItemId)?.item;
            returnItems.Add(new CreateCustomerReturnItemAppDto
            {
                ProductId = item.ProductId,
                DeliveryNoteItemId = item.DeliveryNoteItemId,
                RequestedQuantity = item.RequestedQuantity,
                AcceptedQuantity = conversion.AcceptedQuantity,
                OriginalUnitPrice = deliveryItem?.UnitPrice,
                ReturnUnitPrice = conversion.ReturnUnitPrice
            });
        }

        if (returnItems.Count == 0)
            return CustomerPortalConversionResultAppDto.Fail("Cần có ít nhất một dòng hàng được chấp nhận trả.");

        var createDto = new CreateCustomerReturnAppDto
        {
            DeliveryNoteId = request.DeliveryNoteId,
            CustomerId = request.CustomerId,
            WarehouseId = warehouseId,
            AdditionalCost = additionalCost,
            CompensateInNextDelivery = request.CompensateInNextDelivery,
            ExcludeCustomerReturnRequestId = request.Id,
            Note = BuildConvertedReturnNote(request, adminNote),
            Items = returnItems
        };

        if (createDto.Items.Any(item => item.ReturnUnitPrice < 0))
            return CustomerPortalConversionResultAppDto.Fail("Dữ liệu sản phẩm trả hàng không hợp lệ.");

        var result = await customerReturnAppService.CreateAsync(createDto).ConfigureAwait(false);
        if (!result.Success || !result.CreatedId.HasValue)
            return CustomerPortalConversionResultAppDto.Fail(result.ErrorMessage ?? "Không tạo được phiếu trả hàng.");

        await customerPortalManager.MarkReturnRequestConvertedAsync(request.Id, result.CreatedId.Value, DateTime.UtcNow).ConfigureAwait(false);
        return CustomerPortalConversionResultAppDto.Ok(result.CreatedId.Value, "Đã tạo phiếu trả hàng nội bộ từ yêu cầu portal.");
    }

    public async Task<IReadOnlyCollection<CustomerPortalPaymentIntentAdminAppDto>> GetPaymentIntentsAsync(int? status = null)
    {
        var customers = customerReader.DataSource.ToDictionary(customer => customer.Id);
        var query = paymentIntentReader.DataSource.AsQueryable();

        if (status.HasValue)
            query = query.Where(intent => intent.Status == (CustomerPaymentIntentStatus)status.Value);

        var intents = query
            .OrderByDescending(intent => intent.CreatedOnUtc)
            .ToList();

        var mapped = new List<CustomerPortalPaymentIntentAdminAppDto>();
        foreach (var intent in intents)
            mapped.Add(await MapPaymentIntentAsync(intent, customers.GetValueOrDefault(intent.CustomerId)).ConfigureAwait(false));

        return mapped;
    }

    public async Task<CustomerPortalPaymentIntentAdminAppDto?> GetPaymentIntentAsync(Guid id)
    {
        var intent = await paymentIntentReader.GetByIdAsync(id).ConfigureAwait(false);
        if (intent is null)
            return null;

        var customer = await customerReader.GetByIdAsync(intent.CustomerId).ConfigureAwait(false);
        return await MapPaymentIntentAsync(intent, customer).ConfigureAwait(false);
    }

    public async Task<CustomerActionResultAppDto> ReconcilePaymentIntentAsync(Guid id, string? adminNote)
    {
        var currentUser = await currentUserAccessor.GetCurrentUserAsync().ConfigureAwait(false);
        if (currentUser is null)
            return CustomerActionResultAppDto.Fail("Không xác định được người đối soát.");

        var intent = await customerPortalManager.GetPaymentIntentByIdAsync(id).ConfigureAwait(false);
        if (intent is null)
            return CustomerActionResultAppDto.Fail("Không tìm thấy payment intent.");

        if (intent.Status is not CustomerPaymentIntentStatus.SucceededPendingReconciliation)
            return CustomerActionResultAppDto.Fail("Payment intent chưa ở trạng thái chờ đối soát.");

        CustomerDebtAppDto? debt = null;
        if (intent.CustomerDebtId.HasValue)
        {
            debt = await customerDebtAppService.GetDebtByIdAsync(intent.CustomerDebtId.Value).ConfigureAwait(false);
            if (debt is null || debt.CustomerId != intent.CustomerId)
                return CustomerActionResultAppDto.Fail("Khoản công nợ không hợp lệ.");
        }

        var payment = await customerDebtAppService.RecordPaymentAsync(new CreateCustomerPaymentAppDto
        {
            CustomerId = intent.CustomerId,
            CustomerDebtId = intent.CustomerDebtId,
            OrderId = debt?.OrderId,
            DeliveryNoteId = debt?.DeliveryNoteId,
            Amount = intent.Amount,
            PaymentMethod = (int)PaymentMethod.BankTransfer,
            PaymentType = intent.CustomerDebtId.HasValue ? (int)PaymentType.DebtPayment : (int)PaymentType.General,
            Note = BuildPaymentNote(intent, adminNote),
            PaidOnUtc = DateTime.UtcNow,
            RecordedByUserId = currentUser.Id
        }).ConfigureAwait(false);

        await customerPortalManager.MarkPaymentIntentReconciledAsync(intent.Id, payment.Id, currentUser.Id, DateTime.UtcNow).ConfigureAwait(false);
        return CustomerActionResultAppDto.Ok("Đã đối soát và ghi nhận thanh toán.");
    }

    private static CustomerPortalAccountAdminAppDto MapAccount(CustomerPortalAccount account, Customer? customer)
        => new()
        {
            CustomerId = account.CustomerId,
            CustomerName = customer?.FullName ?? "Khách hàng",
            CustomerPhone = customer?.PhoneNumber,
            CustomerEmail = customer?.Email,
            Status = (int)account.Status,
            HasPassword = !string.IsNullOrWhiteSpace(account.PasswordHash),
            PasswordSetOnUtc = account.PasswordSetOnUtc,
            LastLoginOnUtc = account.LastLoginOnUtc,
            LastKnownLatitude = account.LastKnownLatitude,
            LastKnownLongitude = account.LastKnownLongitude,
            LastKnownLocationAccuracyMeters = account.LastKnownLocationAccuracyMeters,
            LastKnownLocationCapturedOnUtc = account.LastKnownLocationCapturedOnUtc,
            LastKnownLocationSource = account.LastKnownLocationSource,
            CreatedOnUtc = account.CreatedOnUtc,
            UpdatedOnUtc = account.UpdatedOnUtc
        };

    private static CustomerPortalAccountAdminAppDto MapAccount(CustomerPortalAccountDto account, Customer? customer)
        => new()
        {
            CustomerId = account.CustomerId,
            CustomerName = customer?.FullName ?? "Khách hàng",
            CustomerPhone = customer?.PhoneNumber,
            CustomerEmail = customer?.Email,
            Status = (int)account.Status,
            HasPassword = !string.IsNullOrWhiteSpace(account.PasswordHash),
            PasswordSetOnUtc = account.PasswordSetOnUtc,
            LastLoginOnUtc = account.LastLoginOnUtc,
            LastKnownLatitude = account.LastKnownLatitude,
            LastKnownLongitude = account.LastKnownLongitude,
            LastKnownLocationAccuracyMeters = account.LastKnownLocationAccuracyMeters,
            LastKnownLocationCapturedOnUtc = account.LastKnownLocationCapturedOnUtc,
            LastKnownLocationSource = account.LastKnownLocationSource,
            CreatedOnUtc = account.CreatedOnUtc,
            UpdatedOnUtc = account.UpdatedOnUtc
        };

    private static CustomerPortalSettingsAdminAppDto MapSettings(CustomerPortalSettingsDto settings)
        => new()
        {
            OtpEnabled = settings.OtpEnabled,
            UpdatedOnUtc = settings.UpdatedOnUtc,
            UpdatedByUserId = settings.UpdatedByUserId
        };

    private static CustomerPortalSecurityEventAdminAppDto MapSecurityEvent(CustomerSecurityEvent securityEvent, Customer? customer, DeliveryNote? deliveryNote)
        => new()
        {
            Id = securityEvent.Id,
            CustomerId = securityEvent.CustomerId,
            CustomerName = customer?.FullName,
            DeliveryNoteId = securityEvent.DeliveryNoteId,
            DeliveryNoteCode = deliveryNote?.Code,
            EventType = securityEvent.EventType,
            Outcome = (int)securityEvent.Outcome,
            IpAddress = securityEvent.IpAddress,
            UserAgent = securityEvent.UserAgent,
            MetadataJson = securityEvent.MetadataJson,
            CreatedOnUtc = securityEvent.CreatedOnUtc
        };

    private static CustomerPortalOrderRequestAdminAppDto MapOrderRequest(CustomerOrderRequest request, Customer? customer)
        => new()
        {
            Id = request.Id,
            CustomerId = request.CustomerId,
            CustomerName = customer?.FullName ?? "Khách hàng",
            CustomerPhone = customer?.PhoneNumber,
            Code = request.Code,
            Status = (int)request.Status,
            ExpectedShippingDateUtc = request.ExpectedShippingDateUtc,
            ShippingAddress = request.ShippingAddress,
            Note = request.Note,
            AdminNote = request.AdminNote,
            CreatedOnUtc = request.CreatedOnUtc,
            ReviewedOnUtc = request.ReviewedOnUtc,
            ConvertedOrderId = request.ConvertedOrderId,
            Items = request.Items.Select(item => new CustomerPortalOrderRequestItemAdminAppDto
            {
                Id = item.Id,
                ProductId = item.ProductId,
                ProductName = item.ProductName,
                Quantity = item.Quantity,
                UnitPriceSnapshot = item.UnitPriceSnapshot,
                SubTotal = item.SubTotal,
                RequiresPricing = item.UnitPriceSnapshot <= 0
            }).ToList(),
            TotalAmount = request.Items.Sum(item => item.SubTotal),
            RequiresPricing = request.Items.Any(item => item.UnitPriceSnapshot <= 0)
        };

    private static CustomerPortalReturnRequestAdminAppDto MapReturnRequest(CustomerReturnRequest request, Customer? customer, DeliveryNote? deliveryNote)
        => new()
        {
            Id = request.Id,
            CustomerId = request.CustomerId,
            CustomerName = customer?.FullName ?? "Khách hàng",
            CustomerPhone = customer?.PhoneNumber,
            DeliveryNoteId = request.DeliveryNoteId,
            DeliveryNoteCode = deliveryNote?.Code,
            Status = (int)request.Status,
            Reason = request.Reason,
            CompensateInNextDelivery = request.CompensateInNextDelivery,
            AdminNote = request.AdminNote,
            CreatedOnUtc = request.CreatedOnUtc,
            ReviewedOnUtc = request.ReviewedOnUtc,
            ConvertedCustomerReturnId = request.ConvertedCustomerReturnId,
            Items = request.Items.Select(item =>
            {
                var deliveryItem = deliveryNote?.Items.FirstOrDefault(deliveryItem => deliveryItem.Id == item.DeliveryNoteItemId);
                return new CustomerPortalReturnRequestItemAdminAppDto
                {
                    Id = item.Id,
                    DeliveryNoteItemId = item.DeliveryNoteItemId,
                    ProductId = item.ProductId,
                    ProductName = item.ProductName,
                    RequestedQuantity = item.RequestedQuantity,
                    OriginalUnitPrice = deliveryItem?.UnitPrice,
                    Reason = item.Reason
                };
            }).ToList()
        };

    private async Task PopulateReturnEvidencePicturesAsync(CustomerPortalReturnRequestAdminAppDto request)
    {
        var itemIds = request.Items.Select(item => item.Id).ToList();
        if (itemIds.Count == 0)
            return;

        var pictures = returnRequestItemPictureReader.DataSource
            .Where(picture => itemIds.Contains(picture.CustomerReturnRequestItemId))
            .OrderBy(picture => picture.CreatedOnUtc)
            .ToList()
            .GroupBy(picture => picture.CustomerReturnRequestItemId)
            .ToDictionary(group => group.Key, group => group.ToList());

        foreach (var item in request.Items)
        {
            if (!pictures.TryGetValue(item.Id, out var itemPictures))
                continue;

            foreach (var itemPicture in itemPictures)
            {
                var picture = await pictureAppService.GetBase64PictureByIdAsync(itemPicture.PictureId).ConfigureAwait(false);
                item.EvidencePictures.Add(new CustomerPortalReturnRequestEvidencePictureAdminAppDto
                {
                    PictureId = itemPicture.PictureId,
                    PictureUrl = picture?.Base64Value,
                    FileName = picture?.FileName
                });
            }
        }
    }

    private async Task<CustomerPortalPaymentIntentAdminAppDto> MapPaymentIntentAsync(CustomerPaymentIntent intent, Customer? customer)
    {
        CustomerDebtAppDto? debt = null;
        if (intent.CustomerDebtId.HasValue)
            debt = await customerDebtAppService.GetDebtByIdAsync(intent.CustomerDebtId.Value).ConfigureAwait(false);

        return new CustomerPortalPaymentIntentAdminAppDto
        {
            Id = intent.Id,
            CustomerId = intent.CustomerId,
            CustomerName = customer?.FullName ?? "Khách hàng",
            CustomerPhone = customer?.PhoneNumber,
            CustomerDebtId = intent.CustomerDebtId,
            CustomerDebtCode = debt?.Code,
            OrderCode = debt?.OrderCode,
            DeliveryNoteCode = debt?.DeliveryNoteCode,
            Amount = intent.Amount,
            Provider = intent.Provider,
            ProviderIntentId = intent.ProviderIntentId,
            Status = (int)intent.Status,
            FailureReason = intent.FailureReason,
            CreatedOnUtc = intent.CreatedOnUtc,
            CompletedOnUtc = intent.CompletedOnUtc,
            ReconciledOnUtc = intent.ReconciledOnUtc,
            CustomerPaymentId = intent.CustomerPaymentId
        };
    }

    private static string BuildPaymentNote(CustomerPaymentIntentDto intent, string? adminNote)
    {
        var note = $"Đối soát thanh toán online {intent.Provider}";
        if (!string.IsNullOrWhiteSpace(intent.ProviderIntentId))
            note += $" ({intent.ProviderIntentId})";
        if (!string.IsNullOrWhiteSpace(adminNote))
            note += $". {adminNote}";

        return note;
    }

    private static string BuildConvertedOrderNote(CustomerOrderRequestDto request)
    {
        var note = $"Tạo từ yêu cầu Customer Portal {request.Code}.";
        if (!string.IsNullOrWhiteSpace(request.Note))
            note += $" Ghi chú khách: {request.Note}";
        if (!string.IsNullOrWhiteSpace(request.AdminNote))
            note += $" Ghi chú duyệt: {request.AdminNote}";

        return note;
    }

    private async Task NotifyOrderRequestApprovedAsync(CustomerOrderRequestDto request)
    {
        var customer = await customerReader.GetByIdAsync(request.CustomerId).ConfigureAwait(false);
        if (customer is null)
            return;

        var subject = $"Yêu cầu đặt hàng {request.Code} đã được duyệt";
        var message = $"Yêu cầu đặt hàng {request.Code} đã được duyệt. Vui lòng vào Customer Portal để xem giá và xác nhận tạo đơn.";
        var smsSent = await SendNotificationAsync(CustomerOtpChannel.Sms, customer.PhoneNumber, null, message).ConfigureAwait(false);
        var emailSent = await SendNotificationAsync(CustomerOtpChannel.Email, customer.Email, subject, message).ConfigureAwait(false);

        await securityManager.RecordSecurityEventAsync(new CreateCustomerSecurityEventDto
        {
            CustomerId = request.CustomerId,
            EventType = "OrderRequestApprovedNotification",
            Outcome = smsSent || emailSent ? CustomerPortalSecurityEventOutcome.Succeeded : CustomerPortalSecurityEventOutcome.Failed,
            MetadataJson = JsonSerializer.Serialize(new
            {
                orderRequestId = request.Id,
                request.Code,
                smsSent,
                emailSent
            })
        }).ConfigureAwait(false);
    }

    private async Task<bool> SendNotificationAsync(CustomerOtpChannel channel, string? destination, string? subject, string message)
    {
        if (string.IsNullOrWhiteSpace(destination))
            return false;

        var sender = notificationSenders.FirstOrDefault(sender => sender.Channel == (int)channel);
        if (sender is null)
            return false;

        var result = await sender.SendAsync(new CustomerPortalNotificationSendAppDto
        {
            Channel = (int)channel,
            Destination = destination,
            Subject = subject,
            Message = message
        }).ConfigureAwait(false);
        return result.Success;
    }

    private static string BuildConvertedReturnNote(CustomerReturnRequestDto request, string? adminNote)
    {
        var note = $"Tạo từ yêu cầu trả hàng Customer Portal {request.Id}.";
        if (!string.IsNullOrWhiteSpace(request.Reason))
            note += $" Lý do khách: {request.Reason}";
        if (!string.IsNullOrWhiteSpace(request.AdminNote))
            note += $" Ghi chú duyệt: {request.AdminNote}";
        if (!string.IsNullOrWhiteSpace(adminNote))
            note += $" Ghi chú chuyển phiếu: {adminNote}";

        return note;
    }
}
