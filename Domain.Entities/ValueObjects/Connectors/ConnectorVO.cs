namespace Domain.ValueObjects.Connectors;

public class ConnectorVO : ValueObject
{
    public Guid DocType { get; set; }
    public string AppDocNum { get; set; }

    ConnectorVO() { }

    public ConnectorVO(Guid docType, string docNum)
    {
        DocType = docType;
        AppDocNum = docNum;
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return DocType;
        yield return AppDocNum;
    }
}
