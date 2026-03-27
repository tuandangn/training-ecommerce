using Microsoft.Extensions.Options;
using NamEcommerce.Web.Contracts.Configurations;

namespace NamEcommerce.Web.Services;

public interface IInformationService
{
    public string Name { get; }
}

public sealed class InformationService : IInformationService
{
    private readonly InfoOptions _infoOptions;

    public InformationService(InfoOptions infoOptions)
    {
        _infoOptions = infoOptions;
    }

    public string Name => _infoOptions.Name;
}
