using System.Security.Cryptography;
using CRM.Chat.Infrastructure.Contexts;
using CRM.Chat.Infrastructure.Hubs;
using CRM.Chat.Infrastructure.Managers;
using CRM.Chat.Infrastructure.Options;
using CRM.Chat.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using StackExchange.Redis;

namespace CRM.Chat.Infrastructure.DI;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddAuthentication(configuration);
        services.AddRedis(configuration);
        services.AddSignalR();
        services.AddCors();
        services.AddHttpContextAccessor();

        // Register services
        services.AddScoped<IUserContext, UserContext>();
        services.AddSingleton<IRedisManager, RedisManager>();
        services.AddScoped<IChatAssignmentService, ChatAssignmentService>();
        services.AddScoped<ILoadBalancingService, LoadBalancingService>();
        services.AddScoped<INotificationService, NotificationService>();

        // Register hub services
        services.AddSingleton<IChatHub, SignalRChatHub>();
        services.AddSingleton<IOperatorHub, SignalROperatorHub>();

        return services;
    }

    private static IServiceCollection AddAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        // Fix: Use "JwtOptions" section name to match appsettings
        var jwtOptions = configuration.GetSection("JwtOptions").Get<JwtOptions>();

        if (jwtOptions == null || string.IsNullOrEmpty(jwtOptions.PublicKey))
        {
            // Skip JWT configuration if not properly configured
            return services;
        }

        services.Configure<JwtOptions>(configuration.GetSection("JwtOptions"));

        try
        {
            // Convert base64 public key to RSA
            var publicKeyBytes = Convert.FromBase64String(jwtOptions.PublicKey);
            var rsaPublicKey = RSA.Create();
            rsaPublicKey.ImportSubjectPublicKeyInfo(publicKeyBytes, out _);

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = jwtOptions.Issuer,
                        ValidAudience = jwtOptions.Audience,
                        IssuerSigningKey = new RsaSecurityKey(rsaPublicKey),
                        ClockSkew = TimeSpan.Zero
                    };

                    // Configure SignalR JWT authentication
                    options.Events = new JwtBearerEvents
                    {
                        OnMessageReceived = context =>
                        {
                            var accessToken = context.Request.Query["access_token"];
                            var path = context.HttpContext.Request.Path;

                            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                            {
                                context.Token = accessToken;
                            }

                            return Task.CompletedTask;
                        }
                    };
                });
        }
        catch (Exception ex)
        {
            // Log the error and continue without JWT authentication for development
            Console.WriteLine($"JWT configuration error: {ex.Message}");
        }

        return services;
    }

    private static IServiceCollection AddRedis(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Redis") ?? "localhost:6379";

        services.Configure<RedisOptions>(options => { options.ConnectionString = connectionString; });

        services.AddSingleton<IConnectionMultiplexer>(provider =>
        {
            try
            {
                return ConnectionMultiplexer.Connect(connectionString);
            }
            catch (Exception)
            {
                // For development, return a mock connection or handle gracefully
                throw new InvalidOperationException($"Could not connect to Redis at {connectionString}");
            }
        });

        return services;
    }

    private static IServiceCollection AddCors(this IServiceCollection services)
    {
        services.AddCors(options =>
        {
            options.AddPolicy("AllowAll", builder =>
            {
                builder
                    .AllowAnyOrigin()
                    .AllowAnyMethod()
                    .AllowAnyHeader();
            });
        });

        return services;
    }
}