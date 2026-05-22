using MediatR;
using NamEcommerce.Domain.Shared.Dtos.Debts;
using NamEcommerce.Domain.Shared.Events.Returns;
using NamEcommerce.Domain.Shared.Services.Debts;
using NamEcommerce.Domain.Shared.Services.Returns;

namespace NamEcommerce.Application.Services.Events.Returns;

/// <summary>
/// Xử lý sự kiện <see cref="CustomerReturnOverRefunded"/> — tổng giá trị trả hàng vượt quá
/// tổng công nợ còn lại, khách hàng được hoàn tiền mặt.
/// Tạo <c>CustomerRefund</c> với <c>Amount = OverAmount</c> và trạng thái <c>Pending</c>.
/// </summary>
public sealed class CustomerReturnOverRefundedEventHandler(
    ICustomerRefundManager customerRefundManager,
    ICustomerReturnManager customerReturnManager) : INotificationHandler<CustomerReturnOverRefunded>
{
    public async Task Handle(CustomerReturnOverRefunded notification, CancellationToken cancellationToken)
    {
        // Lấy thông tin CustomerReturn để lấy Code + CustomerName
        var customerReturn = await customerReturnManager.GetByIdAsync(notification.CustomerReturnId)
            .ConfigureAwait(false);
        if (customerReturn is null) return;

        await customerRefundManager.CreateAsync(new CreateCustomerRefundDto
        {
            CustomerId = notification.CustomerId,
            CustomerName = customerReturn.CustomerName,
            CustomerReturnId = notification.CustomerReturnId,
            CustomerReturnCode = customerReturn.Code,
            CustomerDebtId = notification.OverRefundedDebtId,
            Amount = notification.OverAmount
        }).ConfigureAwait(false);
    }
}
