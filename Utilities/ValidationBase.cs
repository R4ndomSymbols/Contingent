
namespace Utilities;

public abstract class ValidatedObject {

    protected IEnumerable<ValidationError>? _validationErrors;
    public IReadOnlyCollection<ValidationError> GetErrors() {
        return _validationErrors.ToList().AsReadOnly();
    }
    public bool CheckErrorsExist(){
        return _validationErrors.Any();
    }
    protected void AddError(ValidationError err){
        
        if (_validationErrors.Any(x => x == err)){
            return;
        }
        else{
            _validationErrors.Append(err);
        }
    }
    private void ClearState(string propName){
        _validationErrors = _validationErrors.Where(x => x.PropertyName != propName);
    }
    protected bool PerformValidation(Func<bool> validation, ValidationError err){
        ClearState(err.PropertyName);
        bool validationResult = validation.Invoke();
        if (!validationResult){
            AddError(err);
        }
        return validationResult;
    }
    
}
