using Contingent.SQL;
using Contingent.Models.Domain.Groups;

namespace Contingent.Models.Domain.Flow.History;


public class GroupHistory
{

    private List<StudentFlowRecord> _history;
    private GroupModel _historySubject;
    public GroupHistory(GroupModel model)
    {
        _historySubject = model;
        _history = GetHistory();
    }
    // подгружены приказы и студенты
    public IEnumerable<StudentFlowRecord> GetStateOnDate(DateTime onDate)
    {
        var before = _history.Where(x => x.OrderNullRestrict.EffectiveDate <= onDate);
        List<StudentFlowRecord> stateNow = new List<StudentFlowRecord>();
        foreach (var rec in before)
        {
            var studentHistory = rec.StudentNullRestrict.GetHistory(null);
            var nextChangedOrder = studentHistory.GetNextGroupChangingOrder(_historySubject);
            if (nextChangedOrder is null || nextChangedOrder.EffectiveDate >= onDate)
            {
                stateNow.Add(rec);
            }
        }
        return stateNow;
    }

    private List<StudentFlowRecord> GetHistory()
    {
        var found = FlowHistory.GetRecordsByFilter(
            new QueryLimits(0, 200),
            new HistoryExtractSettings()
            {
                ExtractByGroup = _historySubject,
                ExtractOrders = true,
                ExtractStudents = true,
                SortByStudentNames = true
            }
        );
        return found.ToList();

    }
}