namespace StudentTracking.Models.SQL;

public class Column {

    public readonly string Name;
    public readonly string? Alias;
    public readonly string TableName;

    public Column(string name, string? alias, string tableName){
        Name = name;
        Alias = alias;
        TableName = tableName;
    }

    public override string ToString()
    {
        return TableName + "." + Name +
            (Alias == null ? "" : " AS " + Alias);
    }
}