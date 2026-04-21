using MediatR;
using NamEcommerce.Web.Contracts.Models.Debts;

namespace NamEcommerce.Web.Contracts.Queries.Models.Debts;

/// <summary>Lấy danh sách nhà cung cấp có công nợ (gom nhóm theo NCC).</summary>
public sealed class GetVendorDebtListQuery : IRequest<VendorDebtListModel>
{
    public string? Keywords { get; set; }
    public int PageIndex { get; set; } = 1;
    public int PageSize { get; set; } = 15;
}

/// <summary>Lấy toàn bộ công nợ + tiền ứng trước + lịch sử của 1 nhà cung cấp.</summary>
public sealed class GetVendorDebtDetailsQuery : IRequest<VendorDebtDetailsModel?>
{
    public required Guid VendorId { get; init; }
}

/// <summary>Lấy thông tin phiếu chi để in biên lai.</summary>
public sealed class GetVendorPaymentReceiptQuery : IRequest<VendorPaymentReceiptModel?>
{
    public required Guid PaymentId { get; init; }
}
