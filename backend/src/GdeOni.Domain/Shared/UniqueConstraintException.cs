namespace GdeOni.Domain.Shared;

public sealed class UniqueConstraintException(string? constraintName)
    : Exception($"Unique constraint violated: {constraintName}")
{
    public string? ConstraintName { get; } = constraintName;
}