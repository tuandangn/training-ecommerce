using Microsoft.Extensions.Localization;
using NamEcommerce.Application.Contracts.Localizations;
using NamEcommerce.Web.Resources;

namespace NamEcommerce.Web.Services.Localizations;

public sealed class LocalizationService(IStringLocalizer<SharedResource> localizer) : ILocalizationAppService
{
    public string GetValue(string code)
        => localizer[code];

    public string GetValue(string code, object[] parameters)
        => localizer[code, parameters];
}
