namespace NamEcommerce.Domain.Shared.Exceptions.Media;

[Serializable]
public sealed class PictureDataIsInvalidException(string? message)  : NamEcommerceDomainException("Error.PictureDataIsInvalidException", message);

