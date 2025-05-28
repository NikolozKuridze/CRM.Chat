using CRM.Chat.Application.Common.Abstractions.Repositories;
using CRM.Chat.Application.Common.Persistence;
using CRM.Chat.Persistence.Databases;
using CRM.Chat.Persistence.Repositories.Base;
using CRM.Chat.Persistence.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CRM.Chat.Persistence.DI;

public static class DependencyInjection
{
    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException(
                "DefaultConnection connection string არ არის კონფიგურირებული appsettings.json-ში");
        }

        Console.WriteLine($"Database connection: {connectionString.Replace("Password=Postgres2025", "Password=***")}");

        services.AddDbContext<ApplicationDbContext>((serviceProvider, options) =>
        {
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName);
                npgsqlOptions.CommandTimeout(60); // 60 seconds timeout
            });

            // Development logging
            options.EnableSensitiveDataLogging(false);
            options.EnableDetailedErrors(true);
        });

        // Repository და UnitOfWork რეგისტრაცია
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }
}