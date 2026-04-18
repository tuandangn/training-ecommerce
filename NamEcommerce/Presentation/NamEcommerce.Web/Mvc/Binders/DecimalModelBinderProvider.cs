using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace NamEcommerce.Web.Mvc.Binders;

/// <summary>
/// Provider tự động áp dụng DecimalModelBinder cho mọi property kiểu decimal và decimal?
/// </summary>
public sealed class DecimalModelBinderProvider : IModelBinderProvider
{
    public IModelBinder? GetBinder(ModelBinderProviderContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var type = context.Metadata.ModelType;

        // Áp dụng cho cả decimal và decimal?
        if (type == typeof(decimal) || type == typeof(decimal?))
            return new DecimalModelBinder();

        return null;
    }
}
