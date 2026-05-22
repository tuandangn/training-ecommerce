using MediatR;
using NamEcommerce.Application.Contracts.Dtos.Returns;
using NamEcommerce.Application.Contracts.Returns;
using NamEcommerce.Web.Contracts.Commands.Models.Returns;
using NamEcommerce.Web.Contracts.Models.Returns;
using NamEcommerce.Web.Framework.Services;

namespace NamEcommerce.Web.Framework.Commands.Handlers.Returns;

public sealed class UpdateCustomerReturnHandler : IRequestHandler<UpdateCustomerReturnCommand, UpdateCustomerReturnResultModel>
{
    private readonly ICustomerReturnAppService _customerReturnAppService;

    public UpdateCustomerReturnHandler(ICustomerReturnAppService customerReturnAppService)
    {
        _customerReturnAppService = customerReturnAppService;
    }

    public async Task<UpdateCustomerReturnResultModel> Handle(UpdateCustomerReturnCommand request, CancellationToken cancellationToken)
    {
        var result = await _customerReturnAppService.UpdateAsync(new UpdateCustomerReturnAppDto(request.Id)
        {
            Note = request.Note,
            ReturnDate = DateTimeHelper.ToUniversalTime(request.ReturnDate)
        }).ConfigureAwait(false);

        return new UpdateCustomerReturnResultModel
        {
            Success = result.Success,
            ErrorMessage = result.ErrorMessage
        };
    }
}
