using NamEcommerce.Domain.Shared;
using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Domain.Shared.Exceptions.Catalog;
using NamEcommerce.Domain.Shared.Helpers;

namespace NamEcommerce.Domain.Entities.Catalog;

[Serializable]
public sealed record UnitMeasurement : AppAggregateEntity
{
    internal UnitMeasurement(Guid id, string name) : base(id)
    {
        if (string.IsNullOrEmpty(name))
            throw new UnitMeasurementNameRequiredException();

        Name = name;
        NormalizedName = TextHelper.Normalize(Name);
    }

    public string Name { get; private set; }
    internal string NormalizedName { get; private set; }

    public int DisplayOrder { get; set; }

    #region Methods

    internal async Task SetNameAsync(string name, INameExistCheckingService checker)
    {
        if (string.Equals(Name, name, StringComparison.Ordinal))
            return;

        ArgumentNullException.ThrowIfNull(checker);
        if (string.IsNullOrEmpty(name))
            throw new UnitMeasurementNameRequiredException();

        if (await checker.DoesNameExistAsync(name, Id).ConfigureAwait(false))
            throw new UnitMeasurementNameExistsException(name);

        Name = name;
        NormalizedName = TextHelper.Normalize(name);
    }

    #endregion
}
