namespace NamEcommerce.Application.Contracts.Localizations;

public interface ILocalizationAppService
{
    string GetValue(string code);
    string GetValue(string code, object[] parameters);
}
