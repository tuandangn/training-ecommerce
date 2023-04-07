namespace NamEcommerce.Api.GraphQl.Exceptions;

[Serializable]
public sealed class ServiceCannotResolvedException : Exception
{
    public ServiceCannotResolvedException(string serviceName) : base($"Service cannot resolved: {serviceName}")
    {
    }
}
