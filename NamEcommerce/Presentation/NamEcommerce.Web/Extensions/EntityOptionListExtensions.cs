using Microsoft.AspNetCore.Mvc.Rendering;
using NamEcommerce.Web.Contracts.Models.Common;

namespace NamEcommerce.Web.Extensions;

public static class EntityOptionListExtensions
{
    public static SelectList ToSelectList(this EntityOptionListModel model, Guid? selectedValue = null) 
        => new SelectList(model.Options, "Id", "Name", selectedValue);
}
