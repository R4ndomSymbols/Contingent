namespace Contingent.SQL;

public class Column : IQueryPart
{

    public readonly string? FuncName;
    public readonly string Name;
    public readonly string? Alias;
    public readonly string TableName;
    private Func<string> _strategy;
    private Column()
    {

    }

    public Column(string name, string alias, string tableName)
    {
        Name = name;
        Alias = alias;
        TableName = tableName;
        _strategy = () => TableName + "." + Name + " AS " + Alias;
    }
    public Column(string name, string tableName)
    {
        Name = name;
        Alias = null;
        TableName = tableName;
        _strategy = () => TableName + "." + Name;
    }
    public Column(string funcName, string name, string tableName, string? aliasForFunc)
    {
        FuncName = funcName;
        Name = name;
        Alias = aliasForFunc;
        TableName = tableName;
        _strategy = () => FuncName + "(" + TableName + "." + Name + ")" + (aliasForFunc is not null ? (" AS " + Alias) : " ");
    }
    public static Column GetRaw(string raw)
    {
        var col = new Column();
        col._strategy = () => raw;
        return col;
    }
    public string AsSQLText()
    {
        return _strategy.Invoke();
    }
}