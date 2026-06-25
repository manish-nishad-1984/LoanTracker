namespace LoanTracker.Application.Common;

public class Result<T>
{
    public bool IsSuccess { get; private set; }
    public T? Value { get; private set; }
    public string? Error { get; private set; }
    public int StatusCode { get; private set; }

    private Result(bool isSuccess, T? value, string? error, int statusCode)
    {
        IsSuccess = isSuccess;
        Value = value;
        Error = error;
        StatusCode = statusCode;
    }

    public static Result<T> Success(T value) => new(true, value, null, 200);
    public static Result<T> Created(T value) => new(true, value, null, 201);
    public static Result<T> NotFound(string message) => new(false, default, message, 404);
    public static Result<T> NotFound(string entityName, object key) => new(false, default, $"{entityName} with id '{key}' was not found.", 404);
    public static Result<T> BadRequest(string message) => new(false, default, message, 400);
    public static Result<T> Conflict(string message) => new(false, default, message, 409);
    public static Result<T> Failure(string message, int statusCode = 500) => new(false, default, message, statusCode);
}

public class Result
{
    public bool IsSuccess { get; private set; }
    public string? Error { get; private set; }
    public int StatusCode { get; private set; }

    private Result(bool isSuccess, string? error, int statusCode)
    {
        IsSuccess = isSuccess;
        Error = error;
        StatusCode = statusCode;
    }

    public static Result Success() => new(true, null, 200);
    public static Result NotFound(string message) => new(false, message, 404);
    public static Result NotFound(string entityName, object key) => new(false, $"{entityName} with id '{key}' was not found.", 404);
    public static Result BadRequest(string message) => new(false, message, 400);
    public static Result Conflict(string message) => new(false, message, 409);
    public static Result Failure(string message, int statusCode = 500) => new(false, message, statusCode);
}
