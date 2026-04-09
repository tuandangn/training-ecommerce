using MediatR;
using NamEcommerce.Application.Contracts.Dtos.Orders;
using NamEcommerce.Web.Contracts.Commands.Models.Customers;
using NamEcommerce.Web.Contracts.Models.Customers;
using NamEcommerce.Application.Contracts.Customers;
using NamEcommerce.Application.Contracts.Dtos.Customers;

namespace NamEcommerce.Web.Framework.Commands.Handlers.Customers;

public sealed class CreateCustomerHandler : IRequestHandler<CreateCustomerCommand, CreateCustomerResultModel>
{
    private readonly ICustomerAppService _customerAppService;

    public CreateCustomerHandler(ICustomerAppService customerAppService)
    {
        _customerAppService = customerAppService;
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

        return new CreateCustomerResultModel
        {
            Success = result.Success,
            ErrorMessage = result.ErrorMessage,
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
