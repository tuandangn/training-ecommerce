using MediatR;
using NamEcommerce.Web.Contracts.Commands.Models.Customers;
using NamEcommerce.Web.Contracts.Models.Customers;
using NamEcommerce.Application.Contracts.Customers;
using NamEcommerce.Application.Contracts.Dtos.Customers;
using NamEcommerce.Application.Contracts.Debts;
using NamEcommerce.Application.Contracts.Dtos.Debts;
using NamEcommerce.Web.Contracts.Services;

namespace NamEcommerce.Web.Framework.Commands.Handlers.Customers;

public sealed class CreateCustomerHandler : IRequestHandler<CreateCustomerCommand, CreateCustomerResultModel>
{
    private readonly ICustomerAppService _customerAppService;
    private readonly ICustomerDebtAppService _customerDebtAppService;
    private readonly ICurrentUserService _currentUserService;

    public CreateCustomerHandler(ICustomerAppService customerAppService, ICustomerDebtAppService customerDebtAppService, ICurrentUserService currentUserService)
    {
        _customerAppService = customerAppService;
        _customerDebtAppService = customerDebtAppService;
        _currentUserService = currentUserService;
    }

    public async Task<CreateCustomerResultModel> Handle(CreateCustomerCommand request, CancellationToken cancellationToken)
    {
        var result = await _customerAppService.CreateCustomerAsync(new CreateCustomerAppDto
        {
            FullName = request.FullName,
            PhoneNumber = request.PhoneNumber,
            Email = request.Email,
            Address = request.Address,
            Note = request.Note
        }).ConfigureAwait(false);

        if (!result.Success)
            return new CreateCustomerResultModel
            {
                Success = false,
                ErrorMessage = result.ErrorMessage,
                CreatedId = Guid.Empty
            };

        // Tạo công nợ ban đầu nếu có
        if (request.InitialDebt.HasValue && request.InitialDebt.Value > 0)
        {
            var currentUser = await _currentUserService.GetCurrentUserInfoAsync().ConfigureAwait(false);
            await _customerDebtAppService.CreateInitialDebtAsync(new CreateInitialCustomerDebtAppDto
            {
                CustomerId = result.CreatedId!.Value,
                TotalAmount = request.InitialDebt.Value,
                CreatedByUserId = currentUser?.Id
            }).ConfigureAwait(false);
        }

        return new CreateCustomerResultModel
        {
            Success = true,
            CreatedId = result.CreatedId ?? Guid.Empty
        };
    }
}

public sealed class UpdateCustomerHandler : IRequestHandler<UpdateCustomerCommand, UpdateCustomerResultModel>
{
    private readonly ICustomerAppService _customerAppService;

    public UpdateCustomerHandler(ICustomerAppService customerAppService)
    {
        _customerAppService = customerAppService;
    }

    public async Task<UpdateCustomerResultModel> Handle(UpdateCustomerCommand request, CancellationToken cancellationToken)
    {
        var result = await _customerAppService.UpdateCustomerAsync(new UpdateCustomerAppDto(request.Id)
        {
            FullName = request.FullName,
            PhoneNumber = request.PhoneNumber,
            Email = request.Email,
            Address = request.Address,
            Note = request.Note
        }).ConfigureAwait(false);

        return new UpdateCustomerResultModel
        {
            Success = result.Success,
            ErrorMessage = result.ErrorMessage,
            UpdatedId = result.UpdatedId ?? Guid.Empty
        };
    }
}

public sealed class DeleteCustomerHandler : IRequestHandler<DeleteCustomerCommand, DeleteCustomerResultModel>
{
    private readonly ICustomerAppService _customerAppService;

    public DeleteCustomerHandler(ICustomerAppService customerAppService)
    {
        _customerAppService = customerAppService;
    }

    public async Task<DeleteCustomerResultModel> Handle(DeleteCustomerCommand request, CancellationToken cancellationToken)
    {
        var result = await _customerAppService.DeleteCustomerAsync(request.Id).ConfigureAwait(false);

        return new DeleteCustomerResultModel
        {
            Success = result.Success,
            ErrorMessage = result.ErrorMessage,
            DeletedId = result.DeletedId ?? Guid.Empty
        };
    }
}
