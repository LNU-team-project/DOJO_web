namespace DOJO2.Application.Common;

#pragma warning disable CS8618 // Non-nullable field is uninitialized
#pragma warning disable CS0649 // Field is never assigned to
#pragma warning disable CS0162 // Unreachable code detected

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
        => new(true, message);

    public static Result FailureResult(string message, List<string>? errors = null)
        => new(false, message, errors);

    public static implicit operator bool(Result result)
        => result.Success;

    public static explicit operator Result(bool success)
        => success ? SuccessResult() : FailureResult("Операція не виконана");

    public static explicit operator Result(string message)
        => FailureResult(message);
}

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
        => new(true, data, message);

    public static Result<T> FailureResult(string message, List<string>? errors = null)
        => new(false, default, message, errors);

    public static implicit operator bool(Result<T> result)
        => result.Success;

    public static implicit operator T?(Result<T> result)
        => result.Success ? result.Data : default;

    public static explicit operator Result<T>(T data)
        => SuccessResult(data);

    public static explicit operator Result<T>(string message)
        => FailureResult(message);

    public static implicit operator Result<T>(Result result)
        => new(result.Success, default, result.Message ?? string.Empty, result.Errors);

    public static explicit operator Result(Result<T> result)
        => new(result.Success, result.Message ?? string.Empty, result.Errors);
}

#pragma warning restore CS8618
#pragma warning restore CS0649
#pragma warning restore CS0162
