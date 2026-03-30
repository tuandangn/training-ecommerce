using System.Collections;

namespace NamEcommerce.Web.Contracts.Models.Common;

[Serializable]
public sealed class EntityOptionListModel : IEnumerable<EntityOptionListModel.EntityOptionModel>, IEnumerable
{
    public required IEnumerable<EntityOptionModel> Options { get; set; }

    public IEnumerator<EntityOptionModel> GetEnumerator() => Options.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    [Serializable]
    public sealed class EntityOptionModel
    {
        public required Guid Id { get; set; }
        public required string Name { get; set; }
    }
}
