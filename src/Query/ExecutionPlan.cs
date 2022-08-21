namespace codecrafters_sqlite.Query;

public record ExecutionPlan(int TablePage, int? IndexPage, string? IndexVal);