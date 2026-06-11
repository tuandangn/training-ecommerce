using NamEcommerce.Domain.Shared.Dtos.Common;
using NamEcommerce.Domain.Shared.Dtos.Debts;

namespace NamEcommerce.Domain.Shared.Services.Debts;

public interface IVendorLedgerManager
{
    Task<VendorLedgerEntryDto> RecordChargeAsync(RecordVendorLedgerChargeDto dto);

    Task<VendorLedgerEntryDto> RecordPaymentAsync(RecordVendorLedgerPaymentDto dto);

    Task<VendorLedgerEntryDto> RecordReturnCreditAsync(RecordVendorLedgerReturnCreditDto dto);

    Task<VendorLedgerEntryDto> RecordRefundReceiptAsync(RecordVendorLedgerRefundReceiptDto dto);

    Task<VendorLedgerEntryDto> RecordCorrectionAsync(RecordVendorCorrectionDto dto);

    Task<decimal> GetBalanceAsync(Guid vendorId);

    Task<IPagedDataDto<VendorLedgerStatementEntryDto>> GetStatementAsync(
        Guid vendorId,
        DateTime? from = null,
        DateTime? to = null,
        int pageIndex = 0,
        int pageSize = 30);

    Task<IPagedDataDto<VendorAccountBalanceDto>> GetBalancesAsync(
        string? keywords = null,
        int pageIndex = 0,
        int pageSize = 15);

    Task<VendorAccountBalanceDto?> GetVendorSummaryAsync(Guid vendorId);
}
