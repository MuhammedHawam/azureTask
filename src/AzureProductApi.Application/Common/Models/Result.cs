namespace AzureProductApi.Application.Common.Models;

/// <summary>
/// Represents the result of an operation that can succeed or fail
/// </summary>
/// <typeparam name="T">The type of the success value</typeparam>
public class Result<T>
{
    private Result(bool isSuccess, T? value, string? error, string[]? errors)
    {
        IsSuccess = isSuccess;
        Value = value;
        Error = error;
        Errors = errors ?? Array.Empty<string>();
    }

    /// <summary>
    /// Gets a value indicating whether the operation was successful
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Gets a value indicating whether the operation failed
    /// </summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>
    /// Gets the success value
    /// </summary>
    public T? Value { get; }

    /// <summary>
    /// Gets the primary error message
    /// </summary>
    public string? Error { get; }

    /// <summary>
    /// Gets all error messages
    /// </summary>
    public string[] Errors { get; }

    /// <summary>
    /// Creates a successful result
    /// </summary>
    /// <param name="value">The success value</param>
    /// <returns>A successful result</returns>
    public static Result<T> Success(T value)
    {
        return new Result<T>(true, value, null, null);
    }

    /// <summary>
    /// Creates a failed result with a single error
    /// </summary>
    /// <param name="error">The error message</param>
    /// <returns>A failed result</returns>
    public static Result<T> Failure(string error)
    {
        return new Result<T>(false, default, error, new[] { error });
    }

    /// <summary>
    /// Creates a failed result with multiple errors
    /// </summary>
    /// <param name="errors">The error messages</param>
    /// <returns>A failed result</returns>
    public static Result<T> Failure(string[] errors)
    {
        var primaryError = errors.Length > 0 ? errors[0] : "An error occurred";
        return new Result<T>(false, default, primaryError, errors);
    }

    /// <summary>
    /// Implicitly converts a value to a successful result
    /// </summary>
    /// <param name="value">The value</param>
    public static implicit operator Result<T>(T value)
    {
        return Success(value);
    }
}

/// <summary>
/// Represents the result of an operation without a return value
/// </summary>
public class Result
{
    private Result(bool isSuccess, string? error, string[]? errors)
    {
        IsSuccess = isSuccess;
        Error = error;
        Errors = errors ?? Array.Empty<string>();
    }

    /// <summary>
    /// Gets a value indicating whether the operation was successful
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Gets a value indicating whether the operation failed
    /// </summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>
    /// Gets the primary error message
    /// </summary>
    public string? Error { get; }

    /// <summary>
    /// Gets all error messages
    /// </summary>
    public string[] Errors { get; }

    /// <summary>
    /// Creates a successful result
    /// </summary>
    /// <returns>A successful result</returns>
    public static Result Success()
    {
        return new Result(true, null, null);
    }

    /// <summary>
    /// Creates a failed result with a single error
    /// </summary>
    /// <param name="error">The error message</param>
    /// <returns>A failed result</returns>
    public static Result Failure(string error)
    {
        return new Result(false, error, new[] { error });
    }

    /// <summary>
    /// Creates a failed result with multiple errors
    /// </summary>
    /// <param name="errors">The error messages</param>
    /// <returns>A failed result</returns>
    public static Result Failure(string[] errors)
    {
        var primaryError = errors.Length > 0 ? errors[0] : "An error occurred";
        return new Result(false, primaryError, errors);
    }
}