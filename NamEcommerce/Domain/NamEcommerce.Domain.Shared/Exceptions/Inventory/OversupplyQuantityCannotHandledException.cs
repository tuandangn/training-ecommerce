namespace NamEcommerce.Domain.Shared.Exceptions.Inventory;

[Serializable]
public sealed class OversupplyQuantityCannotHandledException() : NamEcommerceDomainException("Error.OversupplyQuantityCannotHandled");
