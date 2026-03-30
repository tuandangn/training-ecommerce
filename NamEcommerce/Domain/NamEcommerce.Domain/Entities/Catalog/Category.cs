using NamEcommerce.Domain.Shared;
using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Domain.Shared.Exceptions.Catalog;
using NamEcommerce.Domain.Shared.Helpers;

namespace NamEcommerce.Domain.Entities.Catalog;

[Serializable]
public sealed record Category : AppAggregateEntity
{
    internal Category(Guid id, string name) : base(id)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        Name = name;
        NormalizedName = TextHelper.Normalize(Name);
        CreatedOnUtc = DateTime.UtcNow;
    }

    public string Name { get; private set; }
    internal string NormalizedName { get; private set; } = "";
    public int DisplayOrder { get; set; }

    public Guid? ParentId { get; private set; }

    public DateTime CreatedOnUtc { get; }

    #region Methods

    internal async Task SetNameAsync(string name, IExistCheckingService checker)
    {
        if (string.Equals(Name, name, StringComparison.Ordinal))
            return;

        ArgumentNullException.ThrowIfNull(checker);
        ArgumentException.ThrowIfNullOrEmpty(name);

        if (await checker.DoesNameExistAsync(name, Id).ConfigureAwait(false))
            throw new UnitMeasurementNameExistsException(name);

        Name = name;
        NormalizedName = TextHelper.Normalize(name);
    }

    internal void RemoveParent() => ParentId = null;
    internal async Task SetParentAsync(Guid? parentId, IGetByIdService<Category> byIdGetter)
    {
        if (ParentId == parentId)
            return;

        if (!parentId.HasValue)
        {
            RemoveParent();
            return;
        }

        ArgumentNullException.ThrowIfNull(byIdGetter);

        var parent = await byIdGetter.GetByIdAsync(parentId.Value).ConfigureAwait(false);
        if (parent is null)
            throw new CategoryIsNotFoundException(parentId.Value);

        if (parent.ParentId == Id)
            throw new CategoryCircularRelationshipException(Name, parent.Name);

        ParentId = parentId;
    }

    #endregion
}
