using NamEcommerce.Domain.Shared.Exceptions;

namespace NamEcommerce.Domain.Shared.Exceptions.Finance;

public sealed class FixedAssetNotFoundException(Guid id)
    : NamEcommerceDomainException($"Error.FixedAsset.NotFound:{id}");

public sealed class FixedAssetDataInvalidException(string message)
    : NamEcommerceDomainException(message);
