using Microsoft.AspNetCore.Authorization;

namespace NamEcommerce.Web.Controllers;

[Authorize]
public class BaseAuthorizedController : BaseController
{
}
