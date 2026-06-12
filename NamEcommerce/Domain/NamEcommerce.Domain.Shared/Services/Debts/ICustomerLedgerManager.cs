using NamEcommerce.Domain.Shared.Dtos.Common;
using NamEcommerce.Domain.Shared.Dtos.Debts;

namespace NamEcommerce.Domain.Shared.Services.Debts;

public interface ICustomerLedgerManager
{
    Task<CustomerLedgerEntryDto> RecordChargeAsync(RecordCustomerLedgerChargeDto dto);

    Task<CustomerLedgerEntryDto> RecordSurchargeAsync(RecordCustomerLedgerChargeDto dto);

    Task<CustomerLedgerEntryDto> RecordPaymentAsync(RecordCustomerLedgerPaymentDto dto);

    Task<CustomerLedgerEntryDto> RecordReturnCreditAsync(RecordCustomerLedgerReturnCreditDto dto);

    Task<CustomerLedgerEntryDto> RecordRefundPayoutAsync(RecordCustomerLedgerRefundPayoutDto dto);

    Task<CustomerLedgerEntryDto> RecordCorrectionAsync(RecordCustomerCorrectionDto dto);

    Task<decimal> GetBalanceAsync(Guid customerId);

    Task<IPagedDataDto<CustomerLedgerStatementEntryDto>> GetStatementAsync(
        Guid customerId,
        DateTime? from = null,
        DateTime? to = null,
        int pageIndex = 0,
        int pageSize = 30);

    Task<IPagedDataDto<CustomerAccountBalanceDto>> GetBalancesAsync(
        string? keywords = null,
        int pageIndex = 0,
        int pageSize = 15);

    Task<CustomerAccountBalanceDto?> GetCustomerSummaryAsync(Guid customerId);
}
