namespace InspectionService.Shared.Kernel.Common;

/// <summary>
/// Represents the result of an operation with success/failure state and optional error message
/// </summary>
public class Result
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public string Error { get; }

    protected Result(bool isSuccess, string error)
    {
        if (isSuccess && !string.IsNullOrEmpty(error))
            throw new InvalidOperationException("A successful result cannot have an error message.");

        if (!isSuccess && string.IsNullOrEmpty(error))
            throw new InvalidOperationException("A failed result must have an error message.");

        IsSuccess = isSuccess;
        Error = error;
    }

    public static Result Success() => new(true, string.Empty);

    public static Result Failure(string error) => new(false, error);

    public static Result<T> Success<T>(T value) => new(value, true, string.Empty);

    public static Result<T> Failure<T>(string error) => new(default!, false, error);
}

/// <summary>
/// Represents the result of an operation with a value
/// </summary>
public class Result<T> : Result
{
    public T Value { get; }

    protected internal Result(T value, bool isSuccess, string error)
        : base(isSuccess, error)
    {
        Value = value;
    }

    public static implicit operator Result<T>(T value) => Success(value);
}
