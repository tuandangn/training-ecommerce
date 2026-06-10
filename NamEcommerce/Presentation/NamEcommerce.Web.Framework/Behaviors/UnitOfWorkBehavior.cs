using MediatR;
using NamEcommerce.Data.Contracts;

namespace NamEcommerce.Web.Framework.Behaviors;

/// <summary>
/// MediatR pipeline behavior gọi <see cref="IUnitOfWork.CommitAsync"/> sau khi handler hoàn tất.
/// <para>
/// Trong Bước 1 (repo còn autosave): CommitAsync là no-op — không có gì pending trong ChangeTracker.
/// Trong Bước 2+ (repo staged): CommitAsync trở thành điểm save DUY NHẤT, đảm bảo atomicity.
/// </para>
/// </summary>
public sealed class UnitOfWorkBehavior<TRequest, TResponse>(IUnitOfWork unitOfWork)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var response = await next().ConfigureAwait(false);
        await unitOfWork.CommitAsync(cancellationToken).ConfigureAwait(false);
        return response;
    }
}
