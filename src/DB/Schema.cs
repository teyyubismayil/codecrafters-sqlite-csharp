namespace codecrafters_sqlite.DB;


public record Schema(
    string Type,
    string Name,
    string TableName,
    int RootPage,
    string Sql)
{
    /// <summary>Parses a record into a schema</summary>
    public static Schema Parse(Record record)
    {
        var type = record.Columns[0];
        var name = record.Columns[1];
        var tableName = record.Columns[2];
        var rootPage = int.Parse(record.Columns[3]);
        var sql = record.Columns[4];
        
        return new(type, name, tableName, rootPage, sql);
    }
}
