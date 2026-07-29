using NamEcommerce.Application.Contracts.Dtos.Common;
using NamEcommerce.Application.Contracts.Dtos.Orders;

namespace NamEcommerce.Application.Contracts.Orders;

public interface IFastSaleAppService
{
    Task<QuickCreateOrderResultAppDto> QuickCreateOrderAsync(QuickCreateOrderAppDto2 dto);
    Task<CommonActionResultDto> CompleteQuickCreateOrderPaymentAsync(CompleteQuickCreateOrderPaymentAppDto dto);

    Task<QuickSaleResultAppDto> CreateCashQuickSaleAsync(QuickCreateOrderAppDto dto);
    Task<QuickSaleResultAppDto> CreateBankTransferQuickSaleAsync(QuickCreateOrderAppDto dto, Guid paymentIntentId);
    Task<QuickSaleResultAppDto> CreateUnpaidQuickSaleAsync(QuickCreateOrderAppDto dto);
}
