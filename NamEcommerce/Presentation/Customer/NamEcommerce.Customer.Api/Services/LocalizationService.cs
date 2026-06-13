using Microsoft.Extensions.Localization;
using NamEcommerce.Application.Contracts.Localizations;

namespace NamEcommerce.Customer.Api.Services;

public sealed class LocalizationService() : ILocalizationAppService
{
    public string GetValue(string code)
        => code;

    public string GetValue(string code, object[] parameters)
        => string.Format(code, parameters);
}
