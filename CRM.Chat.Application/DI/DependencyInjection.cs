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
        // Register all request handlers
        var handlerTypes = assembly.GetTypes()
            .Where(t => t.GetInterfaces()
                .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequestHandler<,>)))
            .ToList();

        foreach (var handlerType in handlerTypes)
        {
            var interfaceType = handlerType.GetInterfaces()
                .First(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequestHandler<,>));

            services.AddScoped(interfaceType, handlerType);
        }

        services.AddScoped<IMediator, Mediator>();
    }
}