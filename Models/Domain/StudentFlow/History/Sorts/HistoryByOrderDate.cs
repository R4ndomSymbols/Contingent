using StudentTracking.Models.Domain.Flow.Abstract;
using StudentTracking.Models.Domain.Orders;

namespace StudentTracking.Models.Domain.Flow.History;

// порядок хронологический
public class HistoryByOrderEffectiveDate : OrderedHistory{

    private Comparison<StudentFlowRecord> _defaultComparer => (left, right) => {
        var orderLeft = left.ByOrder;
        var orderRigth = right.ByOrder;
        if (orderLeft is null || orderRigth is null){
            throw new Exception("Приказ должен быть указан");
        }
        if (orderLeft.Equals(orderRigth)){
            return 0;
        }
        if (orderLeft.EffectiveDate == orderRigth.EffectiveDate){
            if (orderLeft.OrderCreationDate == orderRigth.OrderCreationDate){
                return 0;
            }
            else if (orderLeft.OrderCreationDate > orderRigth.OrderCreationDate){
                return 1;
            }
            return -1;
        }
        else if (orderLeft.EffectiveDate > orderRigth.EffectiveDate){
            return 1;
        }
        return -1;
    };

    public HistoryByOrderEffectiveDate(IEnumerable<StudentFlowRecord> records) : base(records){
        _history.Sort(_defaultComparer);    
    }

    public HistoryByOrderEffectiveDate() : base(){
        
    }

    public void RemoveOlderOrEqualThan(Order order, Action<StudentFlowRecord> onDelete){
        if (order is null){
            throw new Exception("Приказ должен быть указан");
        }
        for(int i = 0; i < _history.Count; i++){
            if (order.Equals(_history[i].ByOrder)){
                for (int j = _history.Count-1; j > i-1; j--)
                {
                    onDelete.Invoke(_history[j]);
                    _history.RemoveAt(j);
                }
            }
        }
    }

    public StudentFlowRecord? GetClosestBefore(DateTime dateTime){
        StudentFlowRecord? toReturn = null;
        for(int i = _history.Count-1; i >= 0; i--){
            if (_history[i].OrderNullRestict.EffectiveDate < dateTime){
                toReturn = _history[i];
                break;
            }
        }
        return toReturn;
    }
    public StudentFlowRecord? GetClosestBefore(Order byOrder){
        int index = _history.FindIndex(x => x.OrderNullRestict.Equals(byOrder));
        if(index > 0){
            return _history[index - 1];
        }
        else{
            return null;
        }
    }
    public StudentFlowRecord? GetClosestAfter(DateTime dateTime){
        StudentFlowRecord? toReturn = null;
        for(int i = 0; i < _history.Count; i++){
            if (_history[i].OrderNullRestict.EffectiveDate > dateTime){
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
        Console.WriteLine(string.Join("\n", _history.Select(x => x.OrderNullRestict.EffectiveDate)));
        return _history.Last();
    }
}

