namespace Utilities;

public interface IResult {

    public object GetResultObject();
    public IReadOnlyCollection<ValidationError?> GetErrors();

    public bool IsSuccess {get;} 
    public bool IsFailure {get;} 
}