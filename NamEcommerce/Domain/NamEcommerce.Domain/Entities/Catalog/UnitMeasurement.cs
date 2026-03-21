using NamEcommerce.Domain.Shared;
using NamEcommerce.Domain.Shared.Exceptions.Catalog;
using NamEcommerce.Domain.Shared.Helpers;
using NamEcommerce.Domain.Shared.Services;

namespace NamEcommerce.Domain.Entities.Catalog;

[Serializable]
public sealed record UnitMeasurement : AppAggregateEntity
{
    internal UnitMeasurement(Guid id, string name) : base(id)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        Name = name;
        NormalizedName = TextHelper.Normalize(Name);
    }

    public string Name { get; internal set; }
    internal string NormalizedName { get; set; }

    public int DisplayOrder { get; set; }

    #region Methods

    internal async Task SetNameAsync(string name, IUnitMeasurementManager manager)
    {
        if (string.IsNullOrEmpty(name))
            throw new ArgumentNullException(nameof(name));

        if (string.Equals(Name, name, StringComparison.Ordinal))
            return;

        if (await manager.DoesNameExistAsync(name, Id).ConfigureAwait(false))
            throw new UnitMeasurementNameExistsException(name);

        Name = name;
        NormalizedName = TextHelper.Normalize(name);
    }

    #endregion
}
