using MediatR;

namespace Reviews.Application.Common.Messaging;

public interface ICommand : IRequest;

public interface ICommand<out TResponse> : IRequest<TResponse>;
