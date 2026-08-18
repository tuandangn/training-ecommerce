using MediatR;
using NamEcommerce.Data.Contracts;
using NamEcommerce.Web.Contracts.Commands;

namespace NamEcommerce.Web.Framework.Behaviors;

public sealed class UnitOfWorkBehavior<TRequest, TResponse>(IUnitOfWork unitOfWork) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : ICommand<TResponse>
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var response = await next(cancellationToken).ConfigureAwait(false);
        if (response is not ICommandResult { Success: false })
            await unitOfWork.CommitAsync(cancellationToken).ConfigureAwait(false);
        return response;
    }
}
