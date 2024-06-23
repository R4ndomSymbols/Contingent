using Contingent.Models.Domain.Flow.Abstract;
using Contingent.Models.Domain.Orders;
using Npgsql.Replication;

namespace Contingent.Models.Domain.Flow.History;

// порядок хронологический
// от самых ранних, к самым подзним приказам
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
        return Order.OrderByEffectiveDateComparisonAsc(orderLeft, orderRight);
    };
    // история ведется от самых ранних приказов к самым поздним
    public HistoryByOrderEffectiveDateAsc(IEnumerable<StudentFlowRecord> records) : base(records)
    {
        _history.Sort(_defaultComparer);
        Console.WriteLine(string.Join("\n", _history.Select(x => x.ByOrder!.EffectiveDate)));
    }

    public HistoryByOrderEffectiveDateAsc() : base()
    {

    }

    public int Count => _history.Count;

    public void RemoveOlderOrEqualThan(Order order, Action<StudentFlowRecord> onDelete)
    {
        if (order is null)
        {
            throw new Exception("Приказ должен быть указан");
        }
        for (int i = 0; i < _history.Count; i++)
        {
            // т.к. приказы отсортированы по возрастанию
            // ищется такой приказ, который равен текущему
            // и удаляются все последующие
            // и текущий приказ
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

    public override StudentFlowRecord? Last()
    {
        if (_history.Count == 0)
        {
            return null;
        }
        return _history.Last();
    }
    public StudentFlowRecord this[int index]
    {
        get => _history[index];
    }


    // выводит только конечное состояние студента на дату
    // actually, спросить
    public override StudentFlowRecord? On(DateTime onDate)
    {
        for (int position = 0; position < _history.Count; position++)
        {
            var rec = _history[position];
            if (rec.OrderNullRestrict.EffectiveDate > onDate)
            {
                return position > 0 ? rec : null;
            }
        }
        return this.Last();
    }
}

