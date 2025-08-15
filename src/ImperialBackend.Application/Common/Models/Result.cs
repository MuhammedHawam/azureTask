namespace ImperialBackend.Application.Common.Models;

/// <summary>
/// Represents the result of an operation that can succeed or fail
/// </summary>
/// <typeparam name="T">The type of the success value</typeparam>
public class Result<T>
{
    public bool IsSuccess { get; private set; }
    public T? Value { get; private set; }
    public string? Error { get; private set; }

    public static Result<T> Success(T value) => new Result<T> { IsSuccess = true, Value = value };
    public static Result<T> Failure(string error) => new Result<T> { IsSuccess = false, Error = error };
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