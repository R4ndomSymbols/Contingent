using Contingent.Models.Domain.Flow.Abstract;
using Contingent.Models.Domain.Orders;
using Npgsql.Replication;

namespace Contingent.Models.Domain.Flow.History;

// порядок хронологический
public class HistoryByOrderEffectiveDateAsc : OrderedHistory
{

    private Comparison<StudentFlowRecord> _defaultComparer => (left, right) =>
    {
        var orderLeft = left.ByOrder;
        var orderRight = right.ByOrder;
        if (orderLeft is null || orderRight is null)
        {
            throw new Exception("Приказ должен быть указан");
        }
        if (orderLeft.Equals(orderRight))
        {
            return 0;
        }
        if (orderLeft.EffectiveDate == orderRight.EffectiveDate)
        {
            if (orderLeft.OrderCreationDate == orderRight.OrderCreationDate)
            {
                return 0;
            }
            else if (orderLeft.OrderCreationDate > orderRight.OrderCreationDate)
            {
                return 1;
            }
            return -1;
        }
        else if (orderLeft.EffectiveDate > orderRight.EffectiveDate)
        {
            return 1;
        }
        return -1;
    };

    public HistoryByOrderEffectiveDateAsc(IEnumerable<StudentFlowRecord> records) : base(records)
    {
        _history.Sort(_defaultComparer);
    }

    public HistoryByOrderEffectiveDateAsc() : base()
    {

    }

    public void RemoveOlderOrEqualThan(Order order, Action<StudentFlowRecord> onDelete)
    {
        if (order is null)
        {
            throw new Exception("Приказ должен быть указан");
        }
        for (int i = 0; i < _history.Count; i++)
        {
            if (order.Equals(_history[i].ByOrder))
            {
                for (int j = _history.Count - 1; j > i - 1; j--)
                {
                    onDelete.Invoke(_history[j]);
                    _history.RemoveAt(j);
                }
            }
        }
    }

    public StudentFlowRecord? GetClosestBefore(DateTime dateTime)
    {
        StudentFlowRecord? toReturn = null;
        for (int i = _history.Count - 1; i >= 0; i--)
        {
            if (_history[i].OrderNullRestrict.EffectiveDate < dateTime)
            {
                toReturn = _history[i];
                break;
            }
        }
        return toReturn;
    }
    public StudentFlowRecord? GetClosestBefore(Order byOrder)
    {
        int index = _history.FindIndex(x => x.OrderNullRestrict.Equals(byOrder));
        if (index > 0)
        {
            return _history[index - 1];
        }
        else
        {
            return null;
        }
    }
    public StudentFlowRecord? GetClosestAfter(DateTime dateTime)
    {
        StudentFlowRecord? toReturn = null;
        for (int i = 0; i < _history.Count; i++)
        {
            if (_history[i].OrderNullRestrict.EffectiveDate > dateTime)
            {
                toReturn = _history[i];
                break;
            }
        }
        return toReturn;
    }

    public override void Add(StudentFlowRecord record)
    {
        _history.Add(record);
        _history.Sort(_defaultComparer);
    }

    public override StudentFlowRecord Last()
    {
        return _history.Last();
    }
    public StudentFlowRecord this[int index]
    {
        get => _history[index];
    }
}

