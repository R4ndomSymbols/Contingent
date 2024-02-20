
using Microsoft.VisualBasic;
using Utilities;

namespace StudentTracking.Statistics;

public class Filter<T> {
    
    private Func<IEnumerable<T>, IEnumerable<T>> _filter;

    public static Filter<T> Empty => new Filter<T>();

    private Filter(){
        _filter = (source) => source;
    }

    public Filter(Func<IEnumerable<T>, IEnumerable<T>> filter){
        _filter = filter;
    }

    public IEnumerable<T> Execute(IEnumerable<T> source){
        return _filter.Invoke(source);
    }

    public Filter<T> Merge(Filter<T> mergeWith){
        return new Filter<T>(
            (source) => {
                var filteredByThis = _filter.Invoke(source);
                return mergeWith._filter(filteredByThis);
            }
        );
    }


    



}

public class FilterResult<T> {

    private T filtered;
    public T ResultObject {
        get {
            if (IsSuccess){
                return filtered;
            }
            else {
                throw new Exception("При неудаче запросить объект невозможно");
            }
        }
    }
    public bool IsSuccess {get; private set;}
    public bool IsFailure => !IsSuccess;

    private FilterResult (T obj){
        IsSuccess = true;
        filtered = obj;
    }

    public static FilterResult<T> Success (T obj){
        return new FilterResult<T>(obj);
    }


    public object? GetResultObject()
    {
        return ResultObject;
    }

    public IReadOnlyCollection<ValidationError>? GetErrors()
    {
        return null;
    }
}
