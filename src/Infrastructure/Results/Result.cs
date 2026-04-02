namespace DOJO2.Infrastructure.Results;

#pragma warning disable CS8618 // Non-nullable field is uninitialized
#pragma warning disable CS0649 // Field is never assigned to
#pragma warning disable CS0162 // Unreachable code detected

/// <summary>
/// Результат операції без даних
/// </summary>
public class Result
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public List<string> Errors { get; set; }

    public Result(bool success, string message = "", List<string>? errors = null)
    {
        Success = success;
        Message = message;
        Errors = errors ?? new List<string>();
    }

    public static Result SuccessResult(string message = "Операція виконана успішно")
        => new Result(true, message);

    public static Result FailureResult(string message, List<string>? errors = null)
        => new Result(false, message, errors);

    /// <summary>
    /// Неявне приведення Result до bool (перевіряє Success)
    /// </summary>
    public static implicit operator bool(Result result)
        => result.Success;

    /// <summary>
    /// Явне приведення bool до Result
    /// true -> SuccessResult, false -> FailureResult
    /// </summary>
    public static explicit operator Result(bool success)
        => success ? SuccessResult() : FailureResult("Операція не виконана");

    /// <summary>
    /// Явне приведення string до Result (помилка)
    /// </summary>
    public static explicit operator Result(string message)
        => FailureResult(message);
}

/// <summary>
/// Результат операції з даними
/// </summary>
public class Result<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string? Message { get; set; }
    public List<string> Errors { get; set; }

    public Result(bool success, T? data = default, string message = "", List<string>? errors = null)
    {
        Success = success;
        Data = data;
        Message = message;
        Errors = errors ?? new List<string>();
    }

    public static Result<T> SuccessResult(T data, string message = "Операція виконана успішно")
        => new Result<T>(true, data, message);

    public static Result<T> FailureResult(string message, List<string>? errors = null)
        => new Result<T>(false, default, message, errors);

    /// <summary>
    /// Неявне приведення Result<T> до bool (перевіряє Success)
    /// </summary>
    public static implicit operator bool(Result<T> result)
        => result.Success;

    /// <summary>
    /// Неявне приведення Result<T> до T? (повертає Data)
    /// </summary>
    public static implicit operator T?(Result<T> result)
        => result.Success ? result.Data : default;

    /// <summary>
    /// Явне приведення T до Result<T> (успішний результат)
    /// </summary>
    public static explicit operator Result<T>(T data)
        => data != null ? SuccessResult(data) : FailureResult("Дані не можуть бути null");

    /// <summary>
    /// Явне приведення string до Result<T> (помилка)
    /// </summary>
    public static explicit operator Result<T>(string message)
        => FailureResult(message);

    /// <summary>
    /// Неявне приведення Result до Result<T> (конвертація без даних)
    /// </summary>
    public static implicit operator Result<T>(Result result)
        => new Result<T>(result.Success, default, result.Message ?? string.Empty, result.Errors);

    /// <summary>
    /// Явне приведення Result<T> до Result (відкидає дані)
    /// </summary>
    public static explicit operator Result(Result<T> result)
        => new Result(result.Success, result.Message ?? string.Empty, result.Errors);
}

#pragma warning restore CS8618
#pragma warning restore CS0649
#pragma warning restore CS0162

