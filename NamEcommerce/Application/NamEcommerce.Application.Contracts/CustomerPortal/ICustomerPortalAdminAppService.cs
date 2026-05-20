using NamEcommerce.Application.Contracts.Dtos.CustomerPortal;

namespace NamEcommerce.Application.Contracts.CustomerPortal;

public interface ICustomerPortalAdminAppService
{
    Task<CustomerPortalAdminOverviewAppDto> GetOverviewAsync();
    Task<IReadOnlyCollection<CustomerPortalAccountAdminAppDto>> GetAccountsAsync();
    Task<CustomerPortalAccountAdminAppDto?> GetAccountAsync(Guid customerId);
    Task<CustomerActionResultAppDto> BlockAccountAsync(Guid customerId);
    Task<CustomerActionResultAppDto> UnblockAccountAsync(Guid customerId);

    Task<IReadOnlyCollection<CustomerPortalSecurityEventAdminAppDto>> GetSecurityEventsAsync(Guid? customerId = null, int take = 100);

    Task<IReadOnlyCollection<CustomerPortalOrderRequestAdminAppDto>> GetOrderRequestsAsync(int? status = null);
    Task<CustomerPortalOrderRequestAdminAppDto?> GetOrderRequestAsync(Guid id);
    Task<CustomerActionResultAppDto> ApproveOrderRequestAsync(Guid id, string? adminNote);
    Task<CustomerActionResultAppDto> RejectOrderRequestAsync(Guid id, string? adminNote);
    Task<CustomerPortalConversionResultAppDto> ConvertOrderRequestAsync(Guid id);

    Task<IReadOnlyCollection<CustomerPortalReturnRequestAdminAppDto>> GetReturnRequestsAsync(int? status = null);
    Task<CustomerPortalReturnRequestAdminAppDto?> GetReturnRequestAsync(Guid id);
    Task<CustomerActionResultAppDto> AcceptReturnRequestAsync(Guid id, string? adminNote);
    Task<CustomerActionResultAppDto> RejectReturnRequestAsync(Guid id, string? adminNote);
    Task<CustomerPortalConversionResultAppDto> ConvertReturnRequestAsync(Guid id, Guid warehouseId, string? adminNote);

    Task<IReadOnlyCollection<CustomerPortalPaymentIntentAdminAppDto>> GetPaymentIntentsAsync(int? status = null);
    Task<CustomerPortalPaymentIntentAdminAppDto?> GetPaymentIntentAsync(Guid id);
    Task<CustomerActionResultAppDto> ReconcilePaymentIntentAsync(Guid id, string? adminNote);
}
