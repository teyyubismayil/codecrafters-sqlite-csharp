namespace codecrafters_sqlite.Query;

public record CreateQuery(
    string Table,
    Column[] Columns
);
