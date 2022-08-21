using codecrafters_sqlite.Query;


namespace codecrafters_sqlite.DB;


public class Database
{
    private string _path;
    private readonly DatabaseHeader _header;
    
    
    public Database(string path)
    {
        _path = path;
        _header = DatabaseHeader.Parse(path);
        this.Schemata = RecordHelper.GetRecordsByTablePage(
                path, _header.PageSize, 1, new Dictionary<Column, string>())
            .Select(Schema.Parse)
            .ToArray();
    }
    
    
    public Schema[] Schemata { get; }
    

    public Record[] GetRecordsByTableName(string tableName, Dictionary<Column, string> conditions)
    {
        return RecordHelper.GetRecordsByTablePage(
            _path, _header.PageSize, 
            GetSchemaByTableName(tableName).RootPage, conditions);
    }

    public Record[] GetRecordsByRootPage(int rootPage, Dictionary<Column, string> conditions)
    {
        return RecordHelper.GetRecordsByTablePage(
            _path, _header.PageSize, rootPage, conditions);
    }
    
    public Record[] GetRecordsByIndexPage(int indexPage, int tablePage, 
        string indexVal, Dictionary<Column, string> conditions)
    {
        return RecordHelper.GetRecordsByIndexPage(_path, _header.PageSize, indexPage, 
            indexVal, conditions, tablePage);
    }

    public int GetTablesCount()
    {
        return this.Schemata.Length;
    }

    public Schema GetSchemaByTableName(string tableName)
    {
        return this.Schemata
            .First(s => s.Type == "table" && 
                        string.Equals(s.TableName, tableName, StringComparison.OrdinalIgnoreCase));
    }
}