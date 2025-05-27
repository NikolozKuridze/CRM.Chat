using Microsoft.Extensions.DependencyInjection;

namespace CRM.Chat.Application.Common;

public sealed class Mediator : IMediator
{
    private readonly IServiceProvider _serviceProvider;

    public Mediator(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async ValueTask<Result<TResponse>> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        var requestType = request.GetType();
        var handlerType = typeof(IRequestHandler<,>).MakeGenericType(requestType, typeof(TResponse));
        
        var handler = _serviceProvider.GetService(handlerType);
        
        if (handler == null)
        {
            throw new InvalidOperationException($"No handler found for request type {requestType.Name}");
        }

        // Get pipeline behaviors
        var behaviorType = typeof(IPipelineBehavior<,>).MakeGenericType(requestType, typeof(TResponse));
        var behaviors = _serviceProvider.GetServices(behaviorType)
            .Cast<IPipelineBehavior<IRequest<TResponse>, TResponse>>()
            .Reverse()
            .ToList();

        // Build the pipeline
        RequestHandlerDelegate<TResponse> handlerDelegate = () =>
        {
            var handleMethod = handlerType.GetMethod("Handle");
            var task = (ValueTask<Result<TResponse>>)handleMethod!.Invoke(handler, new object[] { request, cancellationToken })!;
            return task;
        };

        var pipeline = behaviors.Aggregate(handlerDelegate, (next, behavior) =>
            () => behavior.Handle((IRequest<TResponse>)request, next, cancellationToken));

        return await pipeline();
    }
}