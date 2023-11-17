
namespace Utilities.Validation;


public class DbValidatedObject : IDbObjectValidated
{
    protected IEnumerable<ValidationError> _errors;
    private bool _synced;
    private RelationTypes _relationBetweenObjAndDB;

    public RelationTypes CurrentState {
        get {
            if (!_synced){
                UpdateObjectIntegrityState();
            }
            return _relationBetweenObjAndDB;
        }
    }

    protected DbValidatedObject(){
        _errors = new List<ValidationError>();
        _synced = false;
        _relationBetweenObjAndDB = RelationTypes.UnboundInvalid;
    }
    protected void SetBound(){
        _relationBetweenObjAndDB = RelationTypes.Bound;
    }
    public bool CheckErrorsExist()
    {
        return _errors.Any();
    }

    public bool CheckIntegrityErrorsExist()
    {
        return _errors.Any(x => x.GetType() == typeof(DbIntegrityValidationError));
    }

    public virtual bool Equals(IDbObjectValidated? other)
    {
        return false;
    }

    public virtual IDbObjectValidated? GetDbRepresentation()
    {
        return null;
    }

    public IReadOnlyCollection<ValidationError> GetErrors()
    {
        return _errors.ToList().AsReadOnly();
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
        _synced = false;
        return validationResult;
    }

    private void UpdateObjectIntegrityState(){
        var alter = this.GetDbRepresentation();
        if (alter == null){
            if (CheckErrorsExist()){
                _relationBetweenObjAndDB = RelationTypes.UnboundInvalid;
            }
            else{
                _relationBetweenObjAndDB = RelationTypes.Pending;
            }
        } 
        else if (alter.Equals(this)){
            _relationBetweenObjAndDB = RelationTypes.Bound;
        }
        else{
            if(CheckIntegrityErrorsExist()){
                _relationBetweenObjAndDB = RelationTypes.Invalid;
            }
            else{
                _relationBetweenObjAndDB = RelationTypes.Modified;
            }
        }
        _synced = true;
    }
}

public enum RelationTypes {
        // айди объекта не представлен в базе и есть ошибки
        UnboundInvalid = 1,
        // ошибок нет, но ID не представлен в базе 
        Pending = 2,
        // ID представлен в базе, ошибок нет, но состояние отличается
        Modified = 3,
        // ID представлен в базе, есть ошибки, состояние отличается 
        Invalid = 4,
        // ID представлен в базе, с идентичным состоянием
        Bound = 5
    }