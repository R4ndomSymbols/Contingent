namespace Utilities;

public class QueryResult<T> {
    
    private bool _found;
    private T _resultObject;
    public bool IsFound {
        get => _found;
    }
    public T ResultObject{
        get {
            if (!_found){
                throw new Exception("Обращение к полю результата в запросе, который ничего не нашел, невозможно");
            }
            return _resultObject;
        }
    }

    private QueryResult(){

    }

    public static QueryResult<T> Found(T obj){
        if (obj is null){
            throw new Exception("Результат запроса не может быть null");
        }
        return new QueryResult<T>(){_resultObject = obj, _found = true};
    }
    public static QueryResult<T> NotFound(){
        return new QueryResult<T>{_found = false};
    }  


}

