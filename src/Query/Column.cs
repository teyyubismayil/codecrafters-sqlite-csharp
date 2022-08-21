namespace codecrafters_sqlite.Query;

public record Column(
    int Index,
    string Name,
    string Definition
)
{
    public bool IsRowId()
    {
        return this.Definition.Contains("integer primary key", StringComparison.OrdinalIgnoreCase);
    }
}
