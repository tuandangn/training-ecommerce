namespace NamEcommerce.Domain.Services.Common;

public sealed class EntityCodeGenerator
{
    private readonly Dictionary<string, int> _last = [];

    public string Next(string prefix, Func<int> dbCountFn)
    {
        if (!_last.TryGetValue(prefix, out var last))
            last = dbCountFn();
        last++;
        _last[prefix] = last;
        return $"{prefix}-{last:D3}";
    }
}
