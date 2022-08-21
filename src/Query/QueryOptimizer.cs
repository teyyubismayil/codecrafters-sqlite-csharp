using codecrafters_sqlite.DB;


namespace codecrafters_sqlite.Query;


public class QueryOptimizer
{
    private readonly Database _database;
    
    
    public QueryOptimizer(Database database)
    {
        _database = database;
    }


    public ExecutionPlan ChoosePlanForSelectQuery(SelectQuery query)
    {
        var schemata = _database.Schemata
            .Where(s => string.Equals(s.TableName, query.From, StringComparison.OrdinalIgnoreCase))
            .ToList();

        var tablePage = schemata
            .First(s => string.Equals(s.Type, "table", StringComparison.OrdinalIgnoreCase))
            .RootPage;
        
        if (query.Where.Count == 0)
        {
            return new ExecutionPlan(tablePage, null, null);
        }

        int indexInd = -1;
        string? indexVal = null;
        
        for (int i = 0; i < schemata.Count; i++)
        {
            var schema = schemata[i];
            if (string.Equals(schema.Type, "index", StringComparison.OrdinalIgnoreCase))
            {
                var whereInd = query.Where.Keys.ToList().FindIndex(colName => 
                    schema.Sql.Contains(colName, StringComparison.OrdinalIgnoreCase));
                if (whereInd != -1)
                {
                    indexInd = i;
                    indexVal = query.Where[query.Where.Keys.ToList()[whereInd]];
                }
            }
        }

        if (indexInd == -1)
        {
            return new ExecutionPlan(tablePage, null, null);
        }
        return new ExecutionPlan(tablePage, schemata[indexInd].RootPage, indexVal);
    }
}