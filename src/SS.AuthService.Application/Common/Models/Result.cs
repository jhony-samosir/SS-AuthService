namespace SS.AuthService.Application.Common.Models;

public class Result<T>
{
    public bool IsSuccess { get; }
    public T? Value { get; }
    public string? ErrorCode { get; }
    public string? ErrorMessage { get; }

    protected Result(bool isSuccess, T? value, string? errorCode = null, string? errorMessage = null)
    {
        IsSuccess = isSuccess;
        Value = value;
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
    }

    public static Result<T> Success(T value) => new(true, value);
    public static Result<T> Failure(string errorCode, string errorMessage) => new(false, default, errorCode, errorMessage);
}

public class Result : Result<bool>
{
    protected Result(bool isSuccess, string? errorCode = null, string? errorMessage = null) 
        : base(isSuccess, isSuccess, errorCode, errorMessage)
    {
    }

    public static Result Success() => new(true);
    public static new Result Failure(string errorCode, string errorMessage) => new(false, errorCode, errorMessage);
}
