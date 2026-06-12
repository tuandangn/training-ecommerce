using MediatR;

namespace NamEcommerce.Web.Contracts.Commands;

public interface ICommand<TResponse> : IRequest<TResponse>;
