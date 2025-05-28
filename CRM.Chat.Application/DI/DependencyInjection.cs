using System.Reflection;
using CRM.Chat.Application.Common;
using CRM.Chat.Application.Common.Behaviors;
using Microsoft.Extensions.DependencyInjection;

namespace CRM.Chat.Application.DI;
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));

        var assembly = Assembly.GetExecutingAssembly();

        services.AddMediatR(assembly); 
        return services;
    }

    private static void AddMediatR(this IServiceCollection services, Assembly assembly)
    { 
        var handlerTypes = assembly.GetTypes()
            .Where(t => !t.IsInterface && !t.IsAbstract && t.GetInterfaces()
                .Any(i => i.IsGenericType && 
                         (i.GetGenericTypeDefinition() == typeof(IRequestHandler<,>) ||
                          i.GetGenericTypeDefinition() == typeof(IQueryHandler<,>) ||
                          i.GetGenericTypeDefinition() == typeof(ICommandHandler<,>))))
            .ToList();

        foreach (var handlerType in handlerTypes)
        { 
            var interfaces = handlerType.GetInterfaces()
                .Where(i => i.IsGenericType && 
                           (i.GetGenericTypeDefinition() == typeof(IRequestHandler<,>) ||
                            i.GetGenericTypeDefinition() == typeof(IQueryHandler<,>) ||
                            i.GetGenericTypeDefinition() == typeof(ICommandHandler<,>)))
                .ToList();

            foreach (var interfaceType in interfaces)
            {
                services.AddScoped(interfaceType, handlerType);
                 
                if (interfaceType.GetGenericTypeDefinition() == typeof(IQueryHandler<,>) ||
                    interfaceType.GetGenericTypeDefinition() == typeof(ICommandHandler<,>))
                {
                    var requestHandlerInterface = typeof(IRequestHandler<,>)
                        .MakeGenericType(interfaceType.GetGenericArguments());
                    services.AddScoped(requestHandlerInterface, handlerType);
                }
            }
        }

        services.AddScoped<IMediator, Mediator>();
    }
}