using NamEcommerce.Web.Services;

namespace NamEcommerce.Web.Extensions;

public static class DecimalExtensions
{
    public static string DisplayCurrency(this decimal value) 
        => DecimalFormatHelper.FormatCurrency(value);
    public static string DisplayCurrency(this decimal? value) 
        => DecimalFormatHelper.FormatCurrency(value);

    public static string DisplayVietnameseCurrencyHint(this decimal value) 
        => DecimalFormatHelper.ToVietnameseCurrencyHint(value);
    public static string DisplayVietnameseCurrencyHint(this decimal? value) 
        => DecimalFormatHelper.ToVietnameseCurrencyHint(value);

    public static string DisplayQuantity(this decimal value) 
        => DecimalFormatHelper.FormatQuantity(value);
    public static string DisplayQuantity(this decimal? value) 
        => DecimalFormatHelper.FormatQuantity(value);
}
