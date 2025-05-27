using System.Security.Cryptography;
using CRM.Chat.Api.Authentication;
using CRM.Chat.Api.Middleware;
using CRM.Chat.Api.Services;
using CRM.Chat.Application.DI;
using CRM.Chat.Infrastructure.DI;
using CRM.Chat.Infrastructure.Hubs.Core;
using CRM.Chat.Infrastructure.Options;
using CRM.Chat.Persistence.DI;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddHttpContextAccessor();

builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration)
    .AddPersistence(builder.Configuration);

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("JwtOptions"));
var jwtOptions = builder.Configuration.GetSection("JwtOptions").Get<JwtOptions>();

if (jwtOptions?.PublicKey != null)
{
    byte[] publicKeyBytes = Convert.FromBase64String(jwtOptions.PublicKey);
    RSA rsaPublicKey = RSA.Create();
    rsaPublicKey.ImportSubjectPublicKeyInfo(publicKeyBytes, out _);

    builder.Services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
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
        });

    builder.Services.AddSingleton<JwtBearerHandler, TokenAuthenticationHandler>();
}
builder.Services.AddRouting(options =>
{
    options.LowercaseUrls = true;
    options.LowercaseQueryStrings = true;
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add background services
builder.Services.AddHostedService<ChatReassignmentBackgroundService>();

// Configure CORS for SignalR
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

// Add logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

var app = builder.Build();

// Configure the HTTP request pipeline
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseHsts();
}

app.UseHttpsRedirection();

// Use CORS before authentication
app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Map SignalR hubs
app.MapHub<ChatHub>("/hubs/chat");
app.MapHub<OperatorHub>("/hubs/operators");

app.Run();