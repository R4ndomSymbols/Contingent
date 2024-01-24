using System.Collections;

namespace Utilities;

public sealed class Result<T> : IResult{

    private readonly bool _isSuccess;
    private readonly IEnumerable<ValidationError> _errors;
    private T? _resultObject;
    public T? ResultObject {
        get {
            if (_isSuccess) {
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
    public IReadOnlyCollection<ValidationError> Errors {
        get {
            if (_isSuccess || _errors is null){
                throw new Exception("Невозможно получить ошибки из Success запроса"); 
            }
            return _errors.ToList().AsReadOnly();
        }
    }

    private Result (T? resultObject){
        _isSuccess = true;
        _resultObject = resultObject;
        _errors = null;
    }

    private Result (IEnumerable<ValidationError> errors){
        _errors = errors;
        _isSuccess = false;
        _resultObject = default(T);
    }

    public static Result<T?> Success(T? obj){
        return new Result<T?>(obj);
    }
    public static Result<T?> Failure(IEnumerable<ValidationError>? errors){
        if (errors is null){
            throw new ArgumentNullException(nameof(errors));
        }
        if (!errors.Any()){
            throw new ArgumentException(nameof(errors));
        }
        return new Result<T?>(errors);
    }
    public static Result<T?> Failure(ValidationError? err){
        if (err is null){
            throw new ArgumentNullException(nameof(err));
        }
        return new Result<T?>(new List<ValidationError>{err});
    }

    public object? GetResultObject()
    {
        return ResultObject;
    }

    public IReadOnlyCollection<ValidationError>? GetErrors()
    {
        return Errors;
    }
}

