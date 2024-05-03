namespace Contingent.SQL;


public class ForeignKey
{
    public string ColumnName { get; private set; }
    public PrimaryKey Reference { get; private set; }
    public ForeignKey(string colName, PrimaryKey reference)
    {
        ColumnName = colName;
        Reference = reference;
    }

    public override bool Equals(object? obj)
    {
        if (obj is null || obj is not ForeignKey)
        {
            return false;
        }
        var cast = (ForeignKey)obj;
        return this.ColumnName == cast.ColumnName && this.Reference.Equals(cast.Reference);
    }

}

public class PrimaryKey
{
    public string ColumnName { get; private set; }
    public SQLTable InTable { get; private set; }
    public PrimaryKey(string colName, SQLTable table)
    {
        ColumnName = colName;
        InTable = table;
    }

}

public class DataColumn
{
    public string ColumnName { get; set; }

    public DataColumn(string colName)
    {
        ColumnName = colName;
    }
}


