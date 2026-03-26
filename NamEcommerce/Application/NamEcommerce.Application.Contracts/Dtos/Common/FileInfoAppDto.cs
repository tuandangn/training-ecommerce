using System;

namespace NamEcommerce.Application.Contracts.Dtos.Common;

[Serializable]
public sealed record FileInfoAppDto
{
    public required byte[] Data { get; init; }
    public required string MimeType { get; init; }
    public string? Extension { get; init; }
    public string? FileName { get; init; }
}
