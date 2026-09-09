using NamEcommerce.Application.Contracts.Dtos.Common;
using NamEcommerce.Application.Contracts.Dtos.Orders;

namespace NamEcommerce.Application.Contracts.Orders;

public interface IQuickCreateOrderAppService
{
    Task<QuickCreateOrderResultAppDto> QuickCreateOrderAsync(QuickCreateOrderAppDto dto);
    Task<CommonActionResultDto> CompleteQuickCreateOrderPaymentAsync(CompleteQuickCreateOrderPaymentAppDto dto);
    Task<CommonActionResultDto> ProcessOrderWithoutPaymentAsync(Guid orderId);
    Task ProcessOrderAsync(Guid orderId);
}
