using NamEcommerce.Domain.Shared;
using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Domain.Shared.Events.Catalog;
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

    /// <summary>
    /// Đánh dấu đơn vị đo vừa được khởi tạo — Manager gọi trước <c>InsertAsync</c>.
    /// Event sẽ được dispatch sau khi <c>SaveChanges</c> thành công bởi <c>DomainEventDispatchInterceptor</c>.
    /// </summary>
    internal void MarkCreated()
        => RaiseDomainEvent(new UnitMeasurementCreated(Id, Name));

    /// <summary>
    /// Đánh dấu đơn vị đo vừa được cập nhật (đổi tên hoặc DisplayOrder) — raise <see cref="UnitMeasurementUpdated"/>.
    /// </summary>
    internal void MarkUpdated()
        => RaiseDomainEvent(new UnitMeasurementUpdated(Id));

    /// <summary>
    /// Đánh dấu đơn vị đo bị xoá — Manager gọi trước khi <c>DeleteAsync</c> để raise <see cref="UnitMeasurementDeleted"/>.
    /// </summary>
    internal void MarkDeleted()
        => RaiseDomainEvent(new UnitMeasurementDeleted(Id, Name));

    #endregion
}
