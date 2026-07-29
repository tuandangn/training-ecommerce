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

    public async Task<string> NextAsync(string prefix, Func<Task<int>> dbCountFn)
    {
        if (!_last.TryGetValue(prefix, out var last))
            last = await dbCountFn().ConfigureAwait(false);
        last++;
        _last[prefix] = last;
        return $"{prefix}-{last:D3}";
    }
}
