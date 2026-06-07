using NamEcommerce.Web.Contracts.Models.Users;

namespace NamEcommerce.Web.Services.Users;

public interface IUserManagementModelFactory
{
    Task<UserRoleListModel> PrepareUserRoleListModel();

    Task<UserRoleAssignmentModel?> PrepareUserRoleAssignmentModel(Guid userId, UserRoleAssignmentModel? oldModel = null);
}
