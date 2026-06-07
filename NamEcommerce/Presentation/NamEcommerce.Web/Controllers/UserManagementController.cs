using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NamEcommerce.Web.Constants;
using NamEcommerce.Web.Contracts.Commands.Models.Users;
using NamEcommerce.Web.Contracts.Models.Users;
using NamEcommerce.Web.Contracts.Queries.Models.Users;
using NamEcommerce.Web.Services.Users;

namespace NamEcommerce.Web.Controllers;

[Authorize(Policy = AuthorizationPolicyNames.ManageUserRoles)]
public sealed class UserManagementController : BaseAuthorizedController
{
    private readonly IMediator _mediator;
    private readonly IUserManagementModelFactory _userManagementModelFactory;

    public UserManagementController(IMediator mediator, IUserManagementModelFactory userManagementModelFactory)
    {
        _mediator = mediator;
        _userManagementModelFactory = userManagementModelFactory;
    }

    public IActionResult Index() => RedirectToAction(nameof(Roles));

    public async Task<IActionResult> Roles()
    {
        if (!await CanManageUserRoles())
            return ForbidRoleManagement();

        var model = await _userManagementModelFactory.PrepareUserRoleListModel();

        return View(model);
    }

    public async Task<IActionResult> EditRoles(Guid id)
    {
        if (!await CanManageUserRoles())
            return ForbidRoleManagement();

        var model = await _userManagementModelFactory.PrepareUserRoleAssignmentModel(id);
        if (model is null)
        {
            NotifyError("Error.UserIsNotFound");
            return RedirectToAction(nameof(Roles));
        }

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> EditRoles(UserRoleAssignmentModel model)
    {
        if (!await CanManageUserRoles())
            return ForbidRoleManagement();

        if (!ModelState.IsValid)
        {
            var preparedModel = await _userManagementModelFactory.PrepareUserRoleAssignmentModel(model.UserId, model);
            if (preparedModel is null)
            {
                NotifyError("Error.UserIsNotFound");
                return RedirectToAction(nameof(Roles));
            }

            return View(preparedModel);
        }

        var result = await _mediator.Send(new UpdateUserRolesCommand(model.UserId, model.RoleIds));
        if (!result.Success)
        {
            AddLocalizedModelError(result.ErrorMessage);
            var preparedModel = await _userManagementModelFactory.PrepareUserRoleAssignmentModel(model.UserId, model);
            if (preparedModel is null)
            {
                NotifyError("Error.UserIsNotFound");
                return RedirectToAction(nameof(Roles));
            }

            return View(preparedModel);
        }

        NotifySuccess("Msg.SaveSuccess");
        return RedirectToAction(nameof(Roles));
    }

    private Task<bool> CanManageUserRoles()
        => _mediator.Send(new CanManageUserRolesQuery());

    private IActionResult ForbidRoleManagement()
    {
        NotifyError("Error.AccessDenied");
        return RedirectToHome();
    }
}
