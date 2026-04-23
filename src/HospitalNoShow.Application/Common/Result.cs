namespace HospitalNoShow.Application.Common;

/// <summary>
/// Result pattern - exception fırlatmak yerine başarı/hata durumunu taşır.
/// </summary>
public class Result<T>
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public T? Value { get; }
    public string? Error { get; }
    public IReadOnlyList<string> Errors { get; }

    private Result(T value)
    {
        IsSuccess = true;
        Value = value;
        Errors = [];
    }

    private Result(string error)
    {
        IsSuccess = false;
        Error = error;
        Errors = [error];
    }

    private Result(IReadOnlyList<string> errors)
    {
        IsSuccess = false;
        Error = errors.FirstOrDefault();
        Errors = errors;
    }

    public static Result<T> Success(T value) => new(value);
    public static Result<T> Failure(string error) => new(error);
    public static Result<T> Failure(IReadOnlyList<string> errors) => new(errors);
}

public class Result
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public string? Error { get; }
    public IReadOnlyList<string> Errors { get; }

    private Result(bool isSuccess, string? error = null)
    {
        IsSuccess = isSuccess;
        Error = error;
        Errors = error is null ? [] : [error];
    }

    private Result(IReadOnlyList<string> errors)
    {
        IsSuccess = false;
        Error = errors.FirstOrDefault();
        Errors = errors;
    }

    public static Result Success() => new(true);
    public static Result Failure(string error) => new(false, error);
    public static Result Failure(IReadOnlyList<string> errors) => new(errors);
}
