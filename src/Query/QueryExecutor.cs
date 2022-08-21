using codecrafters_sqlite.DB;

namespace codecrafters_sqlite.Query;


public class QueryExecutor
{
    private readonly Database _database;
    private readonly QueryOptimizer _optimizer;
    private readonly QueryParser _parser;

    
    public QueryExecutor(
        Database database, 
        QueryOptimizer optimizer, 
        QueryParser parser)
    {
        _database = database;
        _optimizer = optimizer;
        _parser = parser;
    }


    public void Execute(string query)
    {
        var selectQuery = _parser.ParseSelectQuery(query);
        
        var tableSchema = _database.GetSchemaByTableName(selectQuery.From);
        var createQuery = _parser.ParseCreateQuery(tableSchema.Sql);
        
        var conditions = selectQuery.Where.ToDictionary(
            w => createQuery.Columns.First(c => c.Name == w.Key),
            w => w.Value
        );

        var plan = _optimizer.ChoosePlanForSelectQuery(selectQuery);
        Record[] records;
        if (plan.IndexPage != null)
        {
            records = _database.GetRecordsByIndexPage((int)plan.IndexPage, plan.TablePage, plan.IndexVal, conditions);
        }
        else
        {
            records = _database.GetRecordsByRootPage(plan.TablePage, conditions);
        }
        
        if (selectQuery.Columns.Length == 1 && 
            string.Equals(selectQuery.Columns[0], "count(*)", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine(records.Length);
        }
        else
        {
            var selectCols = selectQuery.Columns
                .Select(cn => createQuery.Columns.First(c =>
                    string.Equals(c.Name, cn, StringComparison.OrdinalIgnoreCase)))
                .ToArray();
            printRecords(records, selectCols);
        }
    }

    
    private void printRecords(Record[] records, Column[] columns)
    {
        foreach (var record in records)
        {
            var colValues = columns
                .Select(col =>
                {
                    if (col.IsRowId())
                    {
                        return record.RowId.ToString();
                    }

                    return record.Columns[col.Index];
                })
                .ToArray();
            Console.WriteLine(string.Join("|", colValues));
        }
    }
}