namespace IconReplacer.Core;

public class OperationResult
{
    protected OperationResult(bool succeeded, IconReplacerError error)
    {
        Succeeded = succeeded;
        Error = error;
    }

    public bool Succeeded { get; }

    public IconReplacerError Error { get; }

    public static OperationResult Success() => new(true, IconReplacerError.None);

    public static OperationResult Failure(IconReplacerError error) => new(false, error);
}

public sealed class OperationResult<T> : OperationResult
{
    private OperationResult(T value)
        : base(true, IconReplacerError.None)
    {
        Value = value;
    }

    private OperationResult(IconReplacerError error)
        : base(false, error)
    {
    }

    public T? Value { get; }

    public static OperationResult<T> Success(T value) => new(value);

    public static new OperationResult<T> Failure(IconReplacerError error) => new(error);
}

