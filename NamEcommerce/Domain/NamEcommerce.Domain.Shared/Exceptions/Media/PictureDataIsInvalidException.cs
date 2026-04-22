namespace NamEcommerce.Domain.Shared.Exceptions.Media;

[Serializable]
public sealed class PictureDataIsInvalidException(string errorCode, params object[] parameters) : NamEcommerceDomainException(errorCode, parameters);


