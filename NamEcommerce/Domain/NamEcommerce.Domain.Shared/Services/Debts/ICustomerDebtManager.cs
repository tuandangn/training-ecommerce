using NamEcommerce.Domain.Shared.Dtos.Common;
using NamEcommerce.Domain.Shared.Dtos.Debts;

namespace NamEcommerce.Domain.Shared.Services.Debts;

public interface ICustomerDebtManager
{
    Task<CustomerDebtDto> CreateDebtFromDeliveryNoteAsync(CreateCustomerDebtDto dto);
    
    Task<CustomerPaymentDto> RecordPaymentAsync(CreateCustomerPaymentDto dto);

    /// <summary>
    /// Thanh toán linh động: phân bổ tiền vào các debt còn lại của khách hàng theo FIFO.
    /// </summary>
    Task<IList<CustomerPaymentDto>> RecordFlexiblePaymentForCustomerAsync(CreateCustomerPaymentDto dto);
    
    Task<CustomerDebtDto?> GetDebtByIdAsync(Guid id);

    Task<CustomerPaymentDto?> GetPaymentByIdAsync(Guid paymentId);

    Task<CustomerDebtSummaryDto?> GetCustomerDebtSummaryAsync(Guid customerId);

    /// <summary>Danh sách khách hàng có công nợ, gom nhóm và tính tổng. Dùng cho trang List.</summary>
    Task<IPagedDataDto<CustomerDebtSummaryDto>> GetCustomersWithDebtsAsync(
        string? keywords = null,
        int pageIndex = 0,
        int pageSize = 15);

    /// <summary>Toàn bộ công nợ của 1 khách hàng kèm tiền cọc và lịch sử thanh toán. Dùng cho trang Details.</summary>
    Task<CustomerDebtsByCustomerDto?> GetDebtsByCustomerIdAsync(Guid customerId);

    Task<IPagedDataDto<CustomerDebtDto>> GetDebtsAsync(
        Guid? customerId = null,
        string? keywords = null,
        int pageIndex = 0,
        int pageSize = 15);

    Task<IPagedDataDto<CustomerPaymentDto>> GetPaymentsAsync(
        Guid? customerId = null,
        int pageIndex = 0,
        int pageSize = 15);
}
