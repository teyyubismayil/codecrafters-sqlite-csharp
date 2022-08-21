namespace codecrafters_sqlite.Query;

public record SelectQuery(
    string From, 
    string[] Columns, 
    Dictionary<string, string> Where
);
