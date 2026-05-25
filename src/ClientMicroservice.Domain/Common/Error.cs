namespace ClientMicroservice.Domain.Common;

public sealed record Error(string Code, string Message)
{
    public static readonly Error None = new(string.Empty, string.Empty);
    public static readonly Error Unexpected = new("Unexpected", "An unexpected error occurred.");
}
