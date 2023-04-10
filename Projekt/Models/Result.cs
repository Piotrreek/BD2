namespace Projekt.Models;

public class Result
{
    public bool IsSuccess { get; protected set; }
    public string? Error { get; protected set; }
    
    protected Result(bool isSuccess)
    {
        IsSuccess = isSuccess;
    }
    
    protected Result(bool isSuccess, string error) : this(isSuccess)
    {
        Error = error;
    }

    protected Result()
    { }

    public static Result Success() => new(true);
    public static Result Failure(Exception e) => new (false, e.Message);
    
    public static Result Failure(string error) => new (false, error);
}

public class Result<T> : Result
{
    public T? Content { get; private set; }
    
    private Result(bool isSuccess, T? content) : base(isSuccess)
    {
        Content = content;
    }

    private Result()
    { }

    public new static Result<T> Failure(Exception e) => new() { Error = e.Message, IsSuccess = false };
    public new static Result<T> Failure(string error) => new() { Error = error, IsSuccess = false };
    public static Result<T> Success(T? content) => new(true, content);
}
