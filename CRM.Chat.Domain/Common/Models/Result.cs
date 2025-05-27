
namespace CRM.Chat.Domain.Common.Models;

public class Result<T>
{
    public bool IsSuccess { get; private set; }
    public bool IsFailure => !IsSuccess;
    public T? Value { get; private set; }
    public string ErrorMessage { get; private set; } = string.Empty;
    public string ErrorCode { get; private set; } = string.Empty;
    public Dictionary<string, string[]>? ValidationErrors { get; private set; }

    private Result(bool isSuccess, T? value, string errorMessage, string errorCode)
    {
        IsSuccess = isSuccess;
        Value = value;
        ErrorMessage = errorMessage;
        ErrorCode = errorCode;
    }

    private Result(Dictionary<string, string[]> validationErrors)
    {
        IsSuccess = false;
        ValidationErrors = validationErrors;
        ErrorMessage = "Validation failed";
        ErrorCode = "ValidationFailed";
    }

    public static Result<T> Success(T value) => new(true, value, string.Empty, string.Empty);
    public static Result<T> Failure(string error, string errorCode) => new(false, default, error, errorCode);
    public static Result<T> ValidationFailure(Dictionary<string, string[]> errors) => new(errors);
}

public static class Result
{
    public static Result<T> Success<T>(T value) => Result<T>.Success(value);
    public static Result<T> Failure<T>(string error, string errorCode) => Result<T>.Failure(error, errorCode);

    public static Result<T> ValidationFailure<T>(Dictionary<string, string[]> errors) =>
        Result<T>.ValidationFailure(errors);
}