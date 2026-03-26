using System.Collections;
using static NamEcommerce.Web.Contracts.Models.Common.EntityOptionListModel;

namespace NamEcommerce.Web.Contracts.Models.Common;

[Serializable]
public sealed class EntityOptionListModel : IEnumerable<EntityOptionModel>, IEnumerable
{
    public required IEnumerable<EntityOptionModel> Options { get; set; }
        = Array.Empty<EntityOptionModel>();

    public IEnumerator<EntityOptionModel> GetEnumerator()
        => Options.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    [Serializable]
    public sealed record EntityOptionModel
    {
        public required Guid Id { get; set; }
        public required string Name { get; set; }
    }
}
