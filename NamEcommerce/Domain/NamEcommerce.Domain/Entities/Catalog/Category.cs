using NamEcommerce.Domain.Shared;
using NamEcommerce.Domain.Shared.Exceptions.Catalog;
using NamEcommerce.Domain.Shared.Helpers;
using NamEcommerce.Domain.Shared.Services;
using NamEcommerce.Domain.Shared.Services.Catalog;

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

    public DateTime CreatedOnUtc { get; init; }

    #region Methods

    internal async Task SetNameAsync(string name, ICategoryManager manager)
    {
        ArgumentNullException.ThrowIfNull(manager);
        ArgumentException.ThrowIfNullOrEmpty(name);

        if (string.Equals(Name, name, StringComparison.Ordinal))
            return;

        if (await manager.DoesNameExistAsync(name, Id).ConfigureAwait(false))
            throw new UnitMeasurementNameExistsException(name);

        Name = name;
        NormalizedName = TextHelper.Normalize(name);
    }

    internal void RemoveParent() => ParentId = null;
    internal async Task SetParentAsync(Guid? parentId, IEntityDataReader<Category> dataReader)
    {
        if (!parentId.HasValue)
        {
            RemoveParent();
            return;
        }

        ArgumentNullException.ThrowIfNull(dataReader);

        var parent = await dataReader.GetByIdAsync(parentId.Value).ConfigureAwait(false);
        if (parent == null)
            throw new ArgumentException("Category is not found", nameof(parentId));

        if (parent.ParentId == Id)
            throw new CategoryCircularRelationshipException(Name, parent.Name);

        ParentId = parentId;
    }

    #endregion
}
