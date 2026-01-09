using System.IO;
using Microsoft.Extensions.Logging;

namespace VapourSynthPortable.Services;

/// <summary>
/// Represents the result of an operation that can succeed or fail.
/// Use this for service methods that may fail in expected ways.
/// </summary>
/// <typeparam name="T">The type of value returned on success</typeparam>
public class Result<T>
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public T? Value { get; }
    public string? Error { get; }
    public string? ErrorDetail { get; }
    public Exception? Exception { get; }

    private Result(bool isSuccess, T? value, string? error, string? errorDetail, Exception? exception)
    {
        IsSuccess = isSuccess;
        Value = value;
        Error = error;
        ErrorDetail = errorDetail;
        Exception = exception;
    }

    /// <summary>
    /// Creates a successful result with a value
    /// </summary>
    public static Result<T> Success(T value) => new(true, value, null, null, null);

    /// <summary>
    /// Creates a failed result with an error message
    /// </summary>
    public static Result<T> Failure(string error, string? detail = null) => new(false, default, error, detail, null);

    /// <summary>
    /// Creates a failed result from an exception
    /// </summary>
    public static Result<T> Failure(Exception exception, string? userFriendlyMessage = null)
    {
        var message = userFriendlyMessage ?? GetUserFriendlyMessage(exception);
        return new(false, default, message, exception.Message, exception);
    }

    /// <summary>
    /// Maps a successful result to a new type
    /// </summary>
    public Result<TNew> Map<TNew>(Func<T, TNew> mapper)
    {
        return IsSuccess
            ? Result<TNew>.Success(mapper(Value!))
            : Result<TNew>.Failure(Error!, ErrorDetail);
    }

    /// <summary>
    /// Executes action if result is successful
    /// </summary>
    public Result<T> OnSuccess(Action<T> action)
    {
        if (IsSuccess && Value != null)
            action(Value);
        return this;
    }

    /// <summary>
    /// Executes action if result is a failure
    /// </summary>
    public Result<T> OnFailure(Action<string, string?> action)
    {
        if (IsFailure)
            action(Error!, ErrorDetail);
        return this;
    }

    /// <summary>
    /// Gets the value or throws if failed
    /// </summary>
    public T GetValueOrThrow()
    {
        if (IsFailure)
            throw Exception ?? new InvalidOperationException(Error);
        return Value!;
    }

    /// <summary>
    /// Gets the value or a default if failed
    /// </summary>
    public T? GetValueOrDefault(T? defaultValue = default) => IsSuccess ? Value : defaultValue;

    private static string GetUserFriendlyMessage(Exception ex)
    {
        return ex switch
        {
            FileNotFoundException => "The specified file could not be found.",
            DirectoryNotFoundException => "The specified directory could not be found.",
            UnauthorizedAccessException => "Access to the file or directory is denied.",
            IOException ioEx when ioEx.Message.Contains("being used") => "The file is in use by another process.",
            IOException => "An I/O error occurred while accessing the file.",
            TimeoutException => "The operation timed out.",
            OperationCanceledException => "The operation was cancelled.",
            ArgumentException argEx => $"Invalid argument: {argEx.ParamName}",
            FormatException => "The data format is invalid.",
            InvalidOperationException => "The operation is not valid in the current state.",
            _ => "An unexpected error occurred."
        };
    }

    /// <summary>
    /// Implicit conversion from value to success result
    /// </summary>
    public static implicit operator Result<T>(T value) => Success(value);
}

/// <summary>
/// Represents the result of an operation that doesn't return a value
/// </summary>
public class Result
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public string? Error { get; }
    public string? ErrorDetail { get; }
    public Exception? Exception { get; }

    private Result(bool isSuccess, string? error, string? errorDetail, Exception? exception)
    {
        IsSuccess = isSuccess;
        Error = error;
        ErrorDetail = errorDetail;
        Exception = exception;
    }

    /// <summary>
    /// Creates a successful result
    /// </summary>
    public static Result Success() => new(true, null, null, null);

    /// <summary>
    /// Creates a failed result with an error message
    /// </summary>
    public static Result Failure(string error, string? detail = null) => new(false, error, detail, null);

    /// <summary>
    /// Creates a failed result from an exception
    /// </summary>
    public static Result Failure(Exception exception, string? userFriendlyMessage = null)
    {
        var message = userFriendlyMessage ?? GetUserFriendlyMessage(exception);
        return new(false, message, exception.Message, exception);
    }

    /// <summary>
    /// Executes action if result is successful
    /// </summary>
    public Result OnSuccess(Action action)
    {
        if (IsSuccess)
            action();
        return this;
    }

    /// <summary>
    /// Executes action if result is a failure
    /// </summary>
    public Result OnFailure(Action<string, string?> action)
    {
        if (IsFailure)
            action(Error!, ErrorDetail);
        return this;
    }

    private static string GetUserFriendlyMessage(Exception ex)
    {
        return ex switch
        {
            FileNotFoundException => "The specified file could not be found.",
            DirectoryNotFoundException => "The specified directory could not be found.",
            UnauthorizedAccessException => "Access to the file or directory is denied.",
            IOException ioEx when ioEx.Message.Contains("being used") => "The file is in use by another process.",
            IOException => "An I/O error occurred while accessing the file.",
            TimeoutException => "The operation timed out.",
            OperationCanceledException => "The operation was cancelled.",
            ArgumentException argEx => $"Invalid argument: {argEx.ParamName}",
            FormatException => "The data format is invalid.",
            InvalidOperationException => "The operation is not valid in the current state.",
            _ => "An unexpected error occurred."
        };
    }
}

/// <summary>
/// Extension methods for Result types to integrate with ToastService
/// </summary>
public static class ResultExtensions
{
    /// <summary>
    /// Shows a toast notification based on the result
    /// </summary>
    public static Result<T> ShowToast<T>(this Result<T> result, string? successMessage = null)
    {
        if (result.IsSuccess && successMessage != null)
        {
            ToastService.Instance.ShowSuccess(successMessage);
        }
        else if (result.IsFailure)
        {
            ToastService.Instance.ShowError(result.Error!, result.ErrorDetail);
        }
        return result;
    }

    /// <summary>
    /// Shows a toast notification based on the result
    /// </summary>
    public static Result ShowToast(this Result result, string? successMessage = null)
    {
        if (result.IsSuccess && successMessage != null)
        {
            ToastService.Instance.ShowSuccess(successMessage);
        }
        else if (result.IsFailure)
        {
            ToastService.Instance.ShowError(result.Error!, result.ErrorDetail);
        }
        return result;
    }

    /// <summary>
    /// Shows error toast only if the result is a failure
    /// </summary>
    public static Result<T> ShowErrorToast<T>(this Result<T> result)
    {
        if (result.IsFailure)
        {
            ToastService.Instance.ShowError(result.Error!, result.ErrorDetail);
        }
        return result;
    }

    /// <summary>
    /// Shows error toast only if the result is a failure
    /// </summary>
    public static Result ShowErrorToast(this Result result)
    {
        if (result.IsFailure)
        {
            ToastService.Instance.ShowError(result.Error!, result.ErrorDetail);
        }
        return result;
    }

    /// <summary>
    /// Logs the result using the logging service
    /// </summary>
    public static Result<T> Log<T>(this Result<T> result, Microsoft.Extensions.Logging.ILogger logger, string operationName)
    {
        if (result.IsSuccess)
        {
            logger.LogInformation("{Operation} completed successfully", operationName);
        }
        else
        {
            if (result.Exception != null)
            {
                logger.LogError(result.Exception, "{Operation} failed: {Error}", operationName, result.Error);
            }
            else
            {
                logger.LogWarning("{Operation} failed: {Error}", operationName, result.Error);
            }
        }
        return result;
    }

    /// <summary>
    /// Logs the result using the logging service
    /// </summary>
    public static Result Log(this Result result, Microsoft.Extensions.Logging.ILogger logger, string operationName)
    {
        if (result.IsSuccess)
        {
            logger.LogInformation("{Operation} completed successfully", operationName);
        }
        else
        {
            if (result.Exception != null)
            {
                logger.LogError(result.Exception, "{Operation} failed: {Error}", operationName, result.Error);
            }
            else
            {
                logger.LogWarning("{Operation} failed: {Error}", operationName, result.Error);
            }
        }
        return result;
    }

    /// <summary>
    /// Wraps a potentially throwing operation in a Result
    /// </summary>
    public static Result<T> Try<T>(Func<T> operation, string? errorMessage = null)
    {
        try
        {
            return Result<T>.Success(operation());
        }
        catch (Exception ex)
        {
            return Result<T>.Failure(ex, errorMessage);
        }
    }

    /// <summary>
    /// Wraps a potentially throwing async operation in a Result
    /// </summary>
    public static async Task<Result<T>> TryAsync<T>(Func<Task<T>> operation, string? errorMessage = null)
    {
        try
        {
            return Result<T>.Success(await operation());
        }
        catch (Exception ex)
        {
            return Result<T>.Failure(ex, errorMessage);
        }
    }

    /// <summary>
    /// Wraps a potentially throwing operation in a Result
    /// </summary>
    public static Result Try(Action operation, string? errorMessage = null)
    {
        try
        {
            operation();
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure(ex, errorMessage);
        }
    }

    /// <summary>
    /// Wraps a potentially throwing async operation in a Result
    /// </summary>
    public static async Task<Result> TryAsync(Func<Task> operation, string? errorMessage = null)
    {
        try
        {
            await operation();
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure(ex, errorMessage);
        }
    }
}
