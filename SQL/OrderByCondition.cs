namespace StudentTracking.SQL;

public class OrderByCondition : IQueryPart
{

    private Column _restrictedColumn;
    private OrderByTypes _type;

    public OrderByCondition(Column restricted, OrderByTypes type)
    {
        _restrictedColumn = restricted;
        _type = type;
    }

    public enum OrderByTypes
    {
        ASC = 1,
        DESC = 2
    }
    public string AsSQLText()
    {
        string orderType;
        switch (_type)
        {
            case OrderByTypes.ASC:
                orderType = "ASC";
                break;
            case OrderByTypes.DESC:
                orderType = "DESC";
                break;
            default:
                throw new Exception("Не определен тип сортировки");

        }
        return "ORDER BY " + _restrictedColumn.AsSQLText() + " " + orderType;
    }
}