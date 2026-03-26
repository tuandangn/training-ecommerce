using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;

namespace NamEcommerce.Web.Mvc.Binders;

public sealed class TrimModelBinderProvider : IModelBinderProvider
{
    public IModelBinder? GetBinder(ModelBinderProviderContext context)
    {
        if (context.Metadata.ModelType == typeof(string))
        {
            var loggerFactory = context.Services.GetRequiredService<ILoggerFactory>();
            return new TrimStringModelBinder(new SimpleTypeModelBinder(typeof(string), loggerFactory));
        }
        return null;
    }
}
