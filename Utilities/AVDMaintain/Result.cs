namespace Utilities;

public sealed class Result<T> : IResult{

    private readonly bool _isSuccess;
    private readonly IEnumerable<ValidationError?> _errors;
    private T _resultObject;
    public T ResultObject {
        get {
            if (_isSuccess && _resultObject is not null) {
                return _resultObject; 
            }
            else {
                throw new ArgumentNullException("Невозможно получить результат из Failure");
            }
        }
    }

    public bool IsSuccess {
        get => _isSuccess;
    }
    public bool IsFailure{
        get => !_isSuccess;
    }
    public IReadOnlyCollection<ValidationError?> Errors {
        get {
            if (_isSuccess || _errors is null){
                throw new Exception("Невозможно получить ошибки из Success запроса"); 
            }
            return _errors.ToList().AsReadOnly();
        }
    }

    private Result (T resultObject){
        _isSuccess = true;
        _resultObject = resultObject;
        _errors = Array.Empty<ValidationError>();
    }

    private Result (IEnumerable<ValidationError?> errors){
        _errors = errors;
        _isSuccess = false;
        _resultObject = default;
    }

    public static Result<T> Success(T obj){
        return new Result<T>(obj);
    }
    public static Result<T> Failure(IEnumerable<ValidationError?>? errors){
        if (errors is null){
            throw new ArgumentNullException(nameof(errors));
        }
        if (!errors.Any()){
            throw new ArgumentException(nameof(errors));
        }
        return new Result<T>(errors);
    }
    public static Result<T> Failure(ValidationError? err){
        if (err is null){
            throw new ArgumentNullException(nameof(err));
        }
        return new Result<T>(new List<ValidationError>{err});
    }

    public static Result<T> Failure(IResult errorConatainer)
    {
        return Result<T>.Failure(errorConatainer.GetErrors());
    }

    public Result<U> Retrace<U> (U resultObject){
        if (this.IsFailure){
            return Result<U>.Failure(this.Errors);
        }
        else {
            return Result<U>.Success(resultObject);
        }
    }
    public Result<U> RetraceFailure<U> (){
        if (this.IsFailure){
            return Result<U>.Failure(this.Errors);
        }
        throw new Exception("Нарушение инварианта результата"); 
    } 




    public object GetResultObject()
    {
        return ResultObject;
    }

    public IReadOnlyCollection<ValidationError?> GetErrors()
    {
        return Errors;
    }
}

public class ResultWithoutValue : IResult
{
    private bool _isSuccess;

    private readonly IEnumerable<ValidationError> _errors;
    public bool IsSuccess => _isSuccess;
    public bool IsFailure => !_isSuccess;

    public IReadOnlyCollection<ValidationError> Errors {
        get {
            if (_isSuccess || _errors is null){
                throw new Exception("Невозможно получить ошибки из Success запроса"); 
            }
            return _errors.ToList().AsReadOnly();
        }
    }

    private ResultWithoutValue(IEnumerable<ValidationError> errors){
        _errors = errors;
        _isSuccess = false;
    }
    private ResultWithoutValue(){
        _errors = Array.Empty<ValidationError>();
        _isSuccess = true;
    }

    public static ResultWithoutValue Success(){
        return new ResultWithoutValue();
    }
    public static ResultWithoutValue Failure(IEnumerable<ValidationError>? errors){
        if (errors is null){
            throw new ArgumentNullException(nameof(errors));
        }
        if (!errors.Any()){
            throw new ArgumentException(nameof(errors));
        }
        return new ResultWithoutValue(errors);
    }
    public static ResultWithoutValue Failure(ValidationError? err){
        if (err is null){
            throw new ArgumentNullException(nameof(err));
        }
        return new ResultWithoutValue(new List<ValidationError>{err});
    }

    public Result<U> Retrace<U> (U? resultObject){
        if (this.IsFailure){
            return Result<U>.Failure(this.Errors);
        }
        else {
            return Result<U>.Success(resultObject);
        }
    }

    public object GetResultObject()
    {
        return null;
    }

    public IReadOnlyCollection<ValidationError> GetErrors()
    {
        return Errors;
    }
}





