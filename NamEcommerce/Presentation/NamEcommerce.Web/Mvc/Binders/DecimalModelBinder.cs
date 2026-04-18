using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Globalization;

namespace NamEcommerce.Web.Mvc.Binders;

/// <summary>
/// Xử lý parse decimal từ chuỗi đã format.
/// Hỗ trợ: "3.400.000", "3,400,000", "1.234,56", "1,234.56", "3400000"
/// </summary>
public class DecimalModelBinder : IModelBinder
{
    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        ArgumentNullException.ThrowIfNull(bindingContext);

        var valueProviderResult = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);
        if (valueProviderResult == ValueProviderResult.None)
            return Task.CompletedTask;

        bindingContext.ModelState.SetModelValue(bindingContext.ModelName, valueProviderResult);

        var rawValue = valueProviderResult.FirstValue;

        if (string.IsNullOrWhiteSpace(rawValue))
        {
            bindingContext.Result = ModelBindingResult.Success(null);
            return Task.CompletedTask;
        }

        var normalised = NormaliseDecimalString(rawValue);

        if (decimal.TryParse(normalised, NumberStyles.Number, CultureInfo.InvariantCulture, out var result))
        {
            bindingContext.Result = ModelBindingResult.Success(result);
        }
        else
        {
            bindingContext.ModelState.TryAddModelError(
                bindingContext.ModelName,
                $"Giá trị '{rawValue}' không hợp lệ.");
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Chuẩn hoá về InvariantCulture (dấu chấm thập phân, không phân cách nghìn).
    ///
    /// Quy tắc nhận dạng:
    ///   - Có cả chấm lẫn phẩy → cái nào đứng sau là thập phân
    ///   - Chỉ có chấm hoặc chỉ có phẩy → xem có đúng 3 chữ số sau không:
    ///       đúng 3 → phân cách nghìn, bỏ đi
    ///       không phải 3 → dấu thập phân, giữ/đổi thành chấm
    /// </summary>
    private static string NormaliseDecimalString(string input)
    {
        var s = input.Trim()
                     .Replace(" ", "")
                     .Replace("₫", "")
                     .Replace("$", "")
                     .Replace("đ", "");

        var lastDot = s.LastIndexOf('.');
        var lastComma = s.LastIndexOf(',');

        // Không có dấu phân cách nào → số nguyên thuần
        if (lastDot == -1 && lastComma == -1)
            return s;

        // Có cả chấm lẫn phẩy → cái đứng sau là thập phân
        if (lastDot != -1 && lastComma != -1)
        {
            return lastDot > lastComma
                ? s.Replace(",", "")            // en-US: 1,234.56
                : s.Replace(".", "").Replace(",", "."); // vi-VN: 1.234,56
        }

        // Chỉ có dấu phẩy
        if (lastDot == -1)
        {
            // Đếm số chữ số sau dấu phẩy cuối
            var digitsAfter = s.Length - lastComma - 1;
            // 3 chữ số sau → phân cách nghìn (3,400 hoặc 3,400,000)
            return digitsAfter == 3
                ? s.Replace(",", "")
                : s.Replace(",", ".");
        }

        // Chỉ có dấu chấm
        {
            var digitsAfter = s.Length - lastDot - 1;
            // 3 chữ số sau → phân cách nghìn (3.400 hoặc 3.400.000)
            return digitsAfter == 3
                ? s.Replace(".", "")
                : s; // đã là dấu thập phân dạng InvariantCulture
        }
    }
}
