using Microsoft.Extensions.DependencyInjection;

namespace CRM.Chat.Application.Common;

public sealed class Mediator : IMediator
{
    private readonly IServiceProvider _serviceProvider;

    public Mediator(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async ValueTask<Result<TResponse>> Send<TResponse>(IRequest<TResponse> request,
        CancellationToken cancellationToken = default)
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
        var behaviors = _serviceProvider.GetServices(behaviorType).ToList();

        // Build the pipeline
        RequestHandlerDelegate<TResponse> handlerDelegate = () =>
        {
            var handleMethod = handlerType.GetMethod("Handle");
            var task = (ValueTask<Result<TResponse>>)handleMethod!.Invoke(handler,
                new object[] { request, cancellationToken })!;
            return task;
        };

        // Apply behaviors in reverse order
        var pipeline = behaviors
            .Cast<object>()
            .Reverse()
            .Aggregate(handlerDelegate, (next, behavior) =>
            {
                var behaviorHandleMethod = behavior.GetType().GetMethod("Handle");
                return () =>
                    (ValueTask<Result<TResponse>>)behaviorHandleMethod!.Invoke(behavior,
                        new object[] { request, next, cancellationToken })!;
            });

        return await pipeline();
    }
}