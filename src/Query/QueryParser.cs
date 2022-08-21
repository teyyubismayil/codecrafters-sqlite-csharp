namespace codecrafters_sqlite.Query;


public class QueryParser
{
    public CreateQuery ParseCreateQuery(string query)
    {
        var tableInd = query.IndexOf("table", StringComparison.OrdinalIgnoreCase);
        var table = query[(tableInd + 5)..query.IndexOf('(')].Trim();
        
        var columns = query[(query.IndexOf('(') + 1)..query.IndexOf(')')]
            .Split(",")
            .Select(s => s.Trim())
            .Select((colDef, index) =>
            {
                var ind = colDef.IndexOf(" ", StringComparison.Ordinal);
                return new Column(index, colDef[..ind], colDef[(ind + 1)..]);
            })
            .ToArray();
        
        return new CreateQuery(table, columns);
    }

    public SelectQuery ParseSelectQuery(string query)
    {
        query = query.Trim();
        var fromInd = query.IndexOf("from", StringComparison.OrdinalIgnoreCase);
        
        var from = query[(fromInd + 4)..].Trim().Split(" ")[0];
        
        var columns = query[6..fromInd]
            .Split(",")
            .Select(col => col.Trim())
            .ToArray();
        
        var whereInd = query.IndexOf("where", StringComparison.OrdinalIgnoreCase);
        Dictionary<string, string> where;
        if (whereInd == -1)
        {
            where = new Dictionary<string, string>();
        }
        else
        {
            where = query[(whereInd + 5)..]
                .Trim()
                .Split(new[] {"and", "AND"}, StringSplitOptions.None)
                .Select(cond => cond.Split("="))
                .ToDictionary(
                    cond => cond[0].Trim(),
                    cond =>
                    {
                        var value = cond[1].Trim();
                        if (value.StartsWith("'") && value.EndsWith("'"))
                        {
                            value = value[1..^1];
                        }

                        return value;
                    });
        }

        return new SelectQuery(from, columns, where);
    }
}
