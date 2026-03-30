using System.Collections;

namespace NamEcommerce.Web.Contracts.Models.Common;

[Serializable]
public sealed class CommonOptionListModel : IEnumerable<CommonOptionListModel.CommonOptionItemModel>, IEnumerable
{
    public IEnumerable<CommonOptionItemModel> Items { get; set; } = [];

    public IEnumerator<CommonOptionItemModel> GetEnumerator() => Items.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    [Serializable]
    public sealed record CommonOptionItemModel(string Text, string Value);
}
