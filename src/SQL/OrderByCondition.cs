namespace Contingent.SQL;

public class OrderByCondition : IQueryPart
{

    private List<(Column col, OrderByTypes type)> _restrictedColumns;
    private OrderByCondition()
    {
        _restrictedColumns = new List<(Column, OrderByTypes)>();
    }

    public OrderByCondition(Column restricted, OrderByTypes type) : this()
    {
        AddColumn(restricted, type);
    }

    public void AddColumn(Column restricted, OrderByTypes type)
    {
        _restrictedColumns.Add((restricted, type));
    }

    public enum OrderByTypes
    {
        ASC = 1,
        DESC = 2
    }
    public string AsSQLText()
    {

        return "ORDER BY " + string.Join(",", _restrictedColumns.Select(by => " " + by.col.AsSQLText() + " " + by.type.ToString()));
    }
}