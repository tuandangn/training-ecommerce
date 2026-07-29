using NamEcommerce.Domain.Shared.Enums.Debts;
using NamEcommerce.Domain.Shared.Enums.Orders;

namespace NamEcommerce.Web.Extensions;

public static class BankTransferPaymentIntentStatusExtensions
{

    extension(BankTransferPaymentIntentStatus status)
    {
        public string GetDisplayText() => status switch
        {
            BankTransferPaymentIntentStatus.Pending => "Đang chờ thanh toán",
            BankTransferPaymentIntentStatus.Confirmed => "Đã nhận tiền",
            BankTransferPaymentIntentStatus.ManuallyConfirmed => "Đã xác nhận",
            BankTransferPaymentIntentStatus.Expired => "Hết hạn",
            BankTransferPaymentIntentStatus.Cancelled => "Đã hủy",
            BankTransferPaymentIntentStatus.Consumed => "Đã sử dụng",
            _ => throw new InvalidDataException(nameof(status)),
        };
    }
}
