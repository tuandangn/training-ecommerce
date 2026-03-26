using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace NamEcommerce.Web.Mvc.Binders;

public sealed class TrimStringModelBinder : IModelBinder
{
    private readonly IModelBinder _fallbackBinder;

    public TrimStringModelBinder(IModelBinder fallbackBinder)
    {
        _fallbackBinder = fallbackBinder;
    }

    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        var valueProviderResult = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);

        if (valueProviderResult != ValueProviderResult.None && valueProviderResult.FirstValue is string str)
        {
            bindingContext.Result = ModelBindingResult.Success(str.Trim());
            return Task.CompletedTask;
        }

        return _fallbackBinder.BindModelAsync(bindingContext);
    }
}
