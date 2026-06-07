# Fast Sale Fulfillment And Payment Modes Implementation

## Todo
- [x] Add backend DTO/domain support for fulfillment and payment timing.
- [x] Branch fast sale record creation for delivered/unpaid and not-delivered/deposit cases.
- [x] Update presentation commands, endpoint, and FastCreate UI.
- [x] Build verification.

## Notes
- No test projects will be run for this pass because the user asked to temporarily skip tests.
- `dotnet build NamEcommerce/NamEcommerce.sln` cannot be used in this worktree because the solution includes legacy website project `NamEcommerce.Customer.Client`, which requires .NET Framework MSBuild.
- Verified with `rtk dotnet build NamEcommerce\Presentation\NamEcommerce.Web\NamEcommerce.Web.csproj`: 0 errors, 23 existing warnings.
