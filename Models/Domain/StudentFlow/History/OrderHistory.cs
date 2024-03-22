using System.Collections.ObjectModel;
using StudentTracking.Models.Domain.Orders;
using StudentTracking.SQL;

namespace StudentTracking.Models.Domain.Flow.History;

public class OrderHistory{

    private Order _byOrder;
    private List<StudentFlowRecord> _history;
    public ReadOnlyCollection<StudentFlowRecord> History => _history.AsReadOnly(); 
    public OrderHistory(Order byOrder){
        if (byOrder is null){
            throw new Exception();
        }
        _byOrder = byOrder;
        _history = GetHistory();
    }

    private List<StudentFlowRecord> GetHistory(){
        var found = FlowHistory.GetRecordsByFilter(
            new QueryLimits(0,500),
            new HistoryExtractSettings{
                ExtractByOrder = (_byOrder, FlowHistory.OrderRelationMode.OnlyIncluded),
                ExtractGroups = true,
                ExtractStudents = true,
            }
        );
        return found.ToList();

    }

}
