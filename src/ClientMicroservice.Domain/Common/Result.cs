namespace ClientMicroservice.Domain.Common;

public readonly struct Result<T>
{
    private readonly Error? _error;

    private Result(T value)
    {
        IsSuccess = true;
        Value = value;
        _error = null;
    }

    private Result(Error error)
    {
        IsSuccess = false;
        Value = default!;
        _error = error;
    }

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public T Value { get; }
    public Error Error => _error ?? Error.None;

    public static Result<T> Success(T value) => new(value);
    public static Result<T> Failure(Error error) => new(error);

    public static implicit operator Result<T>(T value) => Success(value);
    public static implicit operator Result<T>(Error error) => Failure(error);
}
