using NamEcommerce.Application.Contracts.Dtos.Common;
using NamEcommerce.Application.Contracts.Dtos.Debts;

namespace NamEcommerce.Application.Contracts.Debts;

public interface ICustomerDebtAppService
{
    Task<CustomerPaymentAppDto> RecordPaymentAsync(CreateCustomerPaymentAppDto dto);

    /// <summary>
    /// Thanh toán linh động: phân bổ tiền vào các debt còn lại của khách hàng theo FIFO.
    /// </summary>
    Task<IList<CustomerPaymentAppDto>> RecordFlexiblePaymentForCustomerAsync(CreateCustomerPaymentAppDto dto);

    Task<CustomerDebtAppDto?> GetDebtByIdAsync(Guid id);

    Task<CustomerPaymentAppDto?> GetPaymentByIdAsync(Guid paymentId);

    Task<CustomerDebtSummaryAppDto?> GetCustomerDebtSummaryAsync(Guid customerId);

    /// <summary>Danh sách khách hàng có công nợ, gom nhóm. Dùng cho trang List.</summary>
    Task<PagedDataAppDto<CustomerDebtSummaryAppDto>> GetCustomersWithDebtsAsync(
        string? keywords = null,
        int pageIndex = 0,
        int pageSize = 15);

    /// <summary>Toàn bộ công nợ + tiền cọc + lịch sử thanh toán của 1 khách hàng. Dùng cho trang Details.</summary>
    Task<CustomerDebtsByCustomerAppDto?> GetDebtsByCustomerIdAsync(Guid customerId);

    Task<PagedDataAppDto<CustomerDebtAppDto>> GetDebtsAsync(
        Guid? customerId = null,
        string? keywords = null,
        int pageIndex = 0,
        int pageSize = 15);

    Task<PagedDataAppDto<CustomerPaymentAppDto>> GetPaymentsAsync(
        Guid? customerId = null,
        int pageIndex = 0,
        int pageSize = 15);
}
