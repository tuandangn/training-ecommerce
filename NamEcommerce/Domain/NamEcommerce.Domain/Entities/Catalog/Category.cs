using NamEcommerce.Domain.Shared;
using NamEcommerce.Domain.Shared.Common;
using NamEcommerce.Domain.Shared.Events.Catalog;
using NamEcommerce.Domain.Shared.Exceptions.Catalog;
using NamEcommerce.Domain.Shared.Helpers;

namespace NamEcommerce.Domain.Entities.Catalog;

[Serializable]
public sealed record Category : AppAggregateEntity
{
    internal Category(Guid id, string name) : base(id)
    {
        if (string.IsNullOrEmpty(name))
            throw new CategoryNameRequiredException();

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

    internal async Task SetNameAsync(string name, INameExistCheckingService checker)
    {
        if (string.Equals(Name, name, StringComparison.Ordinal))
            return;

        ArgumentNullException.ThrowIfNull(checker);
        if (string.IsNullOrEmpty(name))
            throw new CategoryNameRequiredException();

        if (await checker.DoesNameExistAsync(name, Id).ConfigureAwait(false))
            throw new CategoryNameExistsException(name);

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

    /// <summary>
    /// Đánh dấu danh mục vừa được khởi tạo — Manager gọi trước <c>InsertAsync</c>.
    /// Event sẽ được dispatch sau khi <c>SaveChanges</c> thành công bởi <c>DomainEventDispatchInterceptor</c>.
    /// </summary>
    internal void MarkCreated()
        => RaiseDomainEvent(new CategoryCreated(Id, Name, ParentId));

    /// <summary>
    /// Đánh dấu danh mục vừa được cập nhật (tên / DisplayOrder / Parent) — raise <see cref="CategoryUpdated"/>.
    /// </summary>
    internal void MarkUpdated()
        => RaiseDomainEvent(new CategoryUpdated(Id));

    /// <summary>
    /// Đánh dấu chỉ riêng quan hệ cha-con đổi (ví dụ kéo thả trên cây) — raise <see cref="CategoryParentChanged"/>.
    /// </summary>
    internal void MarkParentChanged()
        => RaiseDomainEvent(new CategoryParentChanged(Id, ParentId));

    /// <summary>
    /// Đánh dấu danh mục bị xoá — Manager gọi trước <c>DeleteAsync</c> để raise <see cref="CategoryDeleted"/>.
    /// </summary>
    internal void MarkDeleted()
        => RaiseDomainEvent(new CategoryDeleted(Id, Name));

    #endregion
}
