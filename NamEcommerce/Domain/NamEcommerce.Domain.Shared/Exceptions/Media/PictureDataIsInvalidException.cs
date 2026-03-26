namespace NamEcommerce.Domain.Shared.Exceptions.Media;

[Serializable]
public sealed class PictureDataIsInvalidException(string? message) : Exception(message);
