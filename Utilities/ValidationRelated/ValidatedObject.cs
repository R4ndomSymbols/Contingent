namespace Utilities.Validation;

public class ValidatedObject : IValidatedObject{

    protected IEnumerable<ValidationError> _errors;
    public IReadOnlyCollection<ValidationError> GetErrors() {
        return _errors.ToList().AsReadOnly();
    }
    public bool CheckErrorsExist(){
        return _errors.Any();
    }
    
    protected ValidatedObject(){
        _errors = new List<ValidationError>();
    }

     protected void AddError(ValidationError err){
        
        if (_errors.Any(x => x == err)){
            return;
        }
        else{
            _errors.Append(err);
        }
    }
    private void ClearState(string propName){
        _errors = _errors.Where(x => x.PropertyName != propName);
    }
    public bool PerformValidation(Func<bool> validation, ValidationError err){
        ClearState(err.PropertyName);
        bool validationResult = validation.Invoke();
        if (!validationResult){
            AddError(err);
        }
        return validationResult;
    }

}