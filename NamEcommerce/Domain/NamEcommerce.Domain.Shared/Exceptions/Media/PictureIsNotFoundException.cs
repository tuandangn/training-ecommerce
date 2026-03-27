namespace NamEcommerce.Domain.Shared.Exceptions.Media;

[Serializable]
public sealed class PictureIsNotFoundException(Guid id) : Exception($"Picture with id '{id}' is not found");
