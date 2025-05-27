using System.Net;
using System.Text.Json; 
using CRM.Chat.Domain.Common.Models;

namespace CRM.Chat.Api.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IWebHostEnvironment _environment;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger,
        IWebHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "An unhandled exception occurred");
            await HandleExceptionAsync(context, exception);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        var result = exception switch
        {
            CRM.Chat.Domain.Common.Exceptions.ValidationException validationException => HandleValidationException(
                validationException),
            KeyNotFoundException => Result.Failure<object>("Resource not found", "NotFound"),
            UnauthorizedAccessException => Result.Failure<object>("Unauthorized access", "Unauthorized"),
            InvalidOperationException => Result.Failure<object>(exception.Message, "BadRequest"),
            _ => HandleUnknownException(exception)
        };

        context.Response.StatusCode = result.ErrorCode switch
        {
            "NotFound" => (int)HttpStatusCode.NotFound,
            "Unauthorized" => (int)HttpStatusCode.Unauthorized,
            "Forbidden" => (int)HttpStatusCode.Forbidden,
            "Conflict" => (int)HttpStatusCode.Conflict,
            "ValidationFailed" => (int)HttpStatusCode.BadRequest,
            "PreconditionFailed" => (int)HttpStatusCode.PreconditionFailed,
            "TooManyRequests" => (int)HttpStatusCode.TooManyRequests,
            "PaymentRequired" => (int)HttpStatusCode.PaymentRequired,
            _ => (int)HttpStatusCode.InternalServerError
        };

        var json = JsonSerializer.Serialize(result, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(json);
    }

    private Result<object> HandleValidationException(Domain.Common.Exceptions.ValidationException exception)
    {
        return Result.ValidationFailure<object>(exception.Errors);
    }

    private Result<object> HandleUnknownException(Exception exception)
    {
        var error = _environment.IsDevelopment()
            ? $"{exception.Message} {exception.StackTrace}"
            : "An unexpected error occurred";

        return Result.Failure<object>(error, "InternalServerError");
    }
}