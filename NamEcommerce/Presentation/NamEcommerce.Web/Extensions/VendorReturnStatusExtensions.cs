using NamEcommerce.Domain.Shared.Enums.Returns;

namespace NamEcommerce.Web.Extensions;

public static class VendorReturnStatusExtensions
{
    public static string GetDisplayText(this VendorReturnStatus type) => type switch
    {
        VendorReturnStatus.Draft => "Nháp",
        VendorReturnStatus.Inspecting => "Đang kiểm tra",
        VendorReturnStatus.Confirmed => "Đã xác nhận",
        VendorReturnStatus.Cancelled => "Đã hủy",
        VendorReturnStatus.Reversed => "Đã hoàn tác",
        _ => throw new InvalidOperationException()

    };

    public static string GetDisplayColor(this VendorReturnStatus type) => type switch
    {
        VendorReturnStatus.Draft => "bg-secondary text-light",
        VendorReturnStatus.Inspecting => "bg-warning text-light",
        VendorReturnStatus.Confirmed => "bg-success text-light",
        VendorReturnStatus.Cancelled => "bg-danger text-light",
        VendorReturnStatus.Reversed => "bg-info text-light",
        _ => "bg-light",
    };
}
