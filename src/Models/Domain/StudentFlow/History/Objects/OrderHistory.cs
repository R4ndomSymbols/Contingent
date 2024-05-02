using System.Collections.ObjectModel;
using StudentTracking.Models.Domain.Orders;
using StudentTracking.SQL;

namespace StudentTracking.Models.Domain.Flow.History;

public class OrderHistory
{

    private Order _byOrder;
    private List<StudentFlowRecord> _history;
    public ReadOnlyCollection<StudentFlowRecord> History => _history.AsReadOnly();
    public OrderHistory(Order byOrder)
    {
        if (byOrder is null)
        {
            throw new Exception();
        }
        _byOrder = byOrder;
        _history = GetHistory();
    }

    private List<StudentFlowRecord> GetHistory()
    {
        var found = FlowHistory.GetRecordsByFilter(
            new QueryLimits(0, 500),
            new HistoryExtractSettings
            {
                ExtractByOrder = (_byOrder, OrderRelationMode.OnlyIncluded),
                ExtractGroups = true,
                ExtractStudents = true,
            }
        );
        return found.ToList();
    }

    public static Order? GetAbsoluteLastOrder()
    {
        var orderBy = new OrderByCondition(
            new Column("specified_date", "orders"), OrderByCondition.OrderByTypes.DESC
        );
        orderBy.AddColumn(new Column("creation_timestamp", "orders"), OrderByCondition.OrderByTypes.DESC);

        var found = Order.FindOrders(
            new QueryLimits(0, 1),
            orderBy: orderBy
        ).Result;
        return found.FirstOrDefault();
    }

}
