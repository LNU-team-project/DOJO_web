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
}

#pragma warning restore CS8618
#pragma warning restore CS0649
#pragma warning restore CS0162

