using CRM.Chat.Api.Middleware;
using CRM.Chat.Api.Services;
using CRM.Chat.Application.DI;
using CRM.Chat.Infrastructure.DI;
using CRM.Chat.Infrastructure.Hubs.Core;
using CRM.Chat.Persistence.DI;

var builder = WebApplication.CreateBuilder(args);
 
builder.Services.AddHttpContextAccessor();

builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration)  
    .AddPersistence(builder.Configuration);

builder.Services.AddRouting(options =>
{
    options.LowercaseUrls = true;
    options.LowercaseQueryStrings = true;
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
 
builder.Services.AddHostedService<ChatReassignmentBackgroundService>();
 
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
 
app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
 
app.MapHub<ChatHub>("/hubs/chat");
app.MapHub<OperatorHub>("/hubs/operators");

app.Run();