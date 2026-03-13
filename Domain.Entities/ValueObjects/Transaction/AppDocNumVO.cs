using Ardalis.GuardClauses;

namespace Domain.ValueObjects.Transaction;

public class AppDocNumVO : ValueObject
{
    public string Value { get; private set; }

    AppDocNumVO() { }

    public AppDocNumVO(string value)
    {
        Value = Guard.Against.NullOrEmpty(value, nameof(value), "App Document Number cannot be null or empty");
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

}
