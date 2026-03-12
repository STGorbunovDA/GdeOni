namespace GdeOni.Domain.Shared;

public record SharedId
{
    private SharedId(Guid value)
    {
        Value = value;
    }

    public Guid Value { get;}

    public static SharedId NewSharedId() => new(Guid.NewGuid());
    public static SharedId Empty() => new(Guid.Empty);
}