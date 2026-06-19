namespace ParkingTizimi.Shared.Results;

public class OperationResult
{
    protected OperationResult(bool succeeded, string message)
    {
        Succeeded = succeeded;
        Message = message;
    }

    public bool Succeeded { get; }
    public string Message { get; }

    public static OperationResult Success(string message = "Operation completed successfully.") => new(true, message);

    public static OperationResult Failure(string message) => new(false, message);
}

public sealed class OperationResult<T> : OperationResult
{
    private OperationResult(bool succeeded, T? value, string message)
        : base(succeeded, message)
    {
        Value = value;
    }

    public T? Value { get; }

    public static OperationResult<T> Success(T value, string message = "Operation completed successfully.") => new(true, value, message);

    public new static OperationResult<T> Failure(string message) => new(false, default, message);
}