using System.Collections;

namespace NamEcommerce.Application.Contracts.Dtos.Common;

[Serializable]
public sealed class CommonOptionListDto : IEnumerable<CommonOptionListDto.OptionItemDto>, IEnumerable
{
    public required IEnumerable<OptionItemDto> Items { get; set; }

    public IEnumerator<OptionItemDto> GetEnumerator() => Items.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    [Serializable]
    public sealed record OptionItemDto
    {
        public required string Text { get; set; }
        public required string Value { get; set; }
    }
}
