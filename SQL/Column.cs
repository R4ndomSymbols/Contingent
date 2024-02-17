namespace StudentTracking.SQL;

public class Column : IQueryPart{

    public readonly string Name;
    public readonly string? Alias;
    public readonly string TableName;

    public Column(string name, string? alias, string tableName){
        Name = name;
        Alias = alias;
        TableName = tableName;
    }
    public Column(string name,  string tableName){
        Name = name;
        Alias = null;
        TableName = tableName;
    }
    public string AsSQLText()
    {
        return TableName + "." + Name +
            (Alias == null ? "" : " AS " + Alias);
    }
}