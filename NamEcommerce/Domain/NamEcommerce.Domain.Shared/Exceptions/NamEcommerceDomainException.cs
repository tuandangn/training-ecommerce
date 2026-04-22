using System;

namespace NamEcommerce.Domain.Shared.Exceptions;

/// <summary>
/// Base exception for all Domain and Application exceptions in NamEcommerce.
/// Contains an ErrorCode (used for localization) and optional parameters to format the error message.
/// </summary>
public class NamEcommerceDomainException : Exception
{
    public string ErrorCode { get; }
    public object[] Parameters { get; }

    public NamEcommerceDomainException(string errorCode, params object[] parameters) 
        : base(errorCode)
    {
        ErrorCode = errorCode;
        Parameters = parameters ?? Array.Empty<object>();
    }
}
