namespace NamEcommerce.Domain.Shared.Exceptions.Media;

[Serializable]
public sealed class PictureIsNotFoundException(Guid id)  : NamEcommerceDomainException("Error.PictureIsNotFoundException", id);

