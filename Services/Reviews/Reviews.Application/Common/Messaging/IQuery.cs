using MediatR;

namespace Reviews.Application.Common.Messaging;

public interface IQuery<out TResponse> : IRequest<TResponse>;
