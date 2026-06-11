namespace NamEcommerce.Domain.Shared.Helpers;

public static class NumberHelper
{
    public static bool IsValidDecimalPlace(decimal value, int decimals)
    {
        var result = value * (decimal)Math.Pow(10, decimals);
        return result == Math.Floor(result);
    }
}
