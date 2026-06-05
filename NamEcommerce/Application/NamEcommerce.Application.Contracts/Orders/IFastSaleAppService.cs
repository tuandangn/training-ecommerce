using NamEcommerce.Application.Contracts.Dtos.Orders;

namespace NamEcommerce.Application.Contracts.Orders;

public interface IFastSaleAppService
{
    Task<QuickSaleResultAppDto> CreateCashQuickSaleAsync(CreateQuickSaleAppDto dto);
    Task<QuickSaleResultAppDto> CreateBankTransferQuickSaleAsync(CreateQuickSaleAppDto dto, Guid paymentIntentId);
    Task<QuickSaleResultAppDto> CreateUnpaidQuickSaleAsync(CreateQuickSaleAppDto dto);
}
