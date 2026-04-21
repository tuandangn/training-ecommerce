using MediatR;
using NamEcommerce.Application.Contracts.Dtos.Orders;
using NamEcommerce.Application.Contracts.Orders;
using NamEcommerce.Web.Contracts.Commands.Models.Orders;
using NamEcommerce.Web.Contracts.Models.Common;

namespace NamEcommerce.Web.Framework.Commands.Handlers.Orders;

public sealed class DeleteOrderHandler(IOrderAppService orderAppService) : IRequestHandler<DeleteOrderCommand, CommonActionResultModel>
{
    public async Task<CommonActionResultModel> Handle(DeleteOrderCommand request, CancellationToken cancellationToken)
    {
        var deleteResult = await orderAppService.DeleteOrderAsync(new DeleteOrderAppDto(request.Id)).ConfigureAwait(false);

        return new CommonActionResultModel
        {
            Success = deleteResult.Success,
            ErrorMessage = deleteResult.ErrorMessage
        };
    }
}
