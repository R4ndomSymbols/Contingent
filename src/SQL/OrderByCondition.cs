namespace Contingent.SQL;

public class OrderByCondition : IQueryPart
{

    private List<(Column col, OrderByTypes type, string nullsBehavior)> _restrictedColumns;
    private OrderByCondition()
    {
        _restrictedColumns = new List<(Column, OrderByTypes, string)>();
    }

    public OrderByCondition(Column restricted, OrderByTypes type, bool nullsFirst = false) : this()
    {
        AddColumn(restricted, type, nullsFirst);
    }

    public void AddColumn(Column restricted, OrderByTypes type, bool nullsFirst = false)
    {
        _restrictedColumns.Add((restricted, type, nullsFirst ? "NULLS FIRST" : " NULLS LAST"));
    }

    public enum OrderByTypes
    {
        ASC = 1,
        DESC = 2
    }
    public string AsSQLText()
    {

        return "ORDER BY " + string.Join(",", _restrictedColumns.Select(by => " " + by.col.AsSQLText() + " " + by.type.ToString() + " " + by.nullsBehavior));
    }
}