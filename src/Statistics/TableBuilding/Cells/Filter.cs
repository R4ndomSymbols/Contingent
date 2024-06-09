using Contingent.Utilities;

namespace Contingent.Statistics;

public class Filter<T>
{

    private List<Filter<T>> _sources;
    private Func<IEnumerable<T>, IEnumerable<T>> _filter;
    public static Filter<T> Empty => new Filter<T>();

    private Filter()
    {
        _filter = (source) => source;
        _sources = new List<Filter<T>>();
    }

    public Filter(Func<IEnumerable<T>, IEnumerable<T>> filter)
    {
        _filter = filter;
        /* логирование 
        _filter = (source) => {
            var count = source.Count();
            if (count!=0){
                Console.WriteLine(count + "\t" + _sources.Count());
            }
            return filter.Invoke(source);
        };*/
        _sources = new List<Filter<T>>();

    }

    public IEnumerable<T> Execute(IEnumerable<T> data)
    {
        IEnumerable<T> processed = data;
        foreach (var f in _sources)
        {
            processed = f.Execute(processed);
        }
        return _filter.Invoke(processed);
    }

    public Filter<T> Include(Filter<T> source)
    {
        var toReturn = Empty;
        toReturn._sources.Add(source);
        toReturn._sources.Add(this);

        if (ReferenceEquals(this, source))
        {
            throw new Exception("Filter can appear only once in a tree");
        }
        CheckFilterDuplicates(this);
        return toReturn;
    }

    private void CheckFilterDuplicates(Filter<T> instance)
    {
        if (_sources.Any(s => ReferenceEquals(s, instance)))
        {
            throw new Exception("Filter can appear only once in a tree");
        }
        foreach (var s in _sources)
        {
            s.CheckFilterDuplicates(instance);
        }
    }






}

public class FilterResult<T>
{

    private T filtered;
    public T ResultObject
    {
        get
        {
            if (IsSuccess)
            {
                return filtered;
            }
            else
            {
                throw new Exception("При неудаче запросить объект невозможно");
            }
        }
    }
    public bool IsSuccess { get; private set; }
    public bool IsFailure => !IsSuccess;

    private FilterResult(T obj)
    {
        IsSuccess = true;
        filtered = obj;
    }

    public static FilterResult<T> Success(T obj)
    {
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
