
using Utilities;

namespace StudentTracking.Statistics;

public class Filter<T> {
    
    private Func<T, T?> filter;
    public Filter(Func<T, T?> filter){
    
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
