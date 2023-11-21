
namespace Utilities.Validation;


public class DbValidatedObject : IDbObjectValidated
{
    private List<ValidationError> _errors;
    private Dictionary<string, int> _invokationLog;
    private bool _synced;
    private RelationTypes _relationBetweenObjAndDB;

    private bool ValidationProperlyInvoked {
        get => _invokationLog.All(x => x.Value > 0);
    }

    public RelationTypes CurrentState {
        get {
            UpdateObjectIntegrityState();
            //Console.WriteLine(string.Join("\n", _invokationLog.Select(x => x.Key + " " + x.Value)));
            return _relationBetweenObjAndDB;
        }
    }

    protected DbValidatedObject(){
        _errors = new List<ValidationError>();
        _invokationLog = new Dictionary<string, int>();
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
        _errors.Add(err);
    }
    private void ClearState(string propName){
        _errors = _errors.Where(x => x.PropertyName != propName).ToList();
    }
    public bool PerformValidation(Func<bool> validation, ValidationError err){
        ClearState(err.PropertyName);
        bool validationResult = validation.Invoke();
        try {
            _invokationLog[err.PropertyName] = _invokationLog[err.PropertyName] + 1;
        }
        catch (Exception){
            throw new InvalidOperationException("Поле не зарегистрировано");
        }
        
        if (!validationResult){
            AddError(err);
        }
        _synced = false;
        return validationResult;
    }
    protected void RegisterProperty(string? name){
        if (name == null){
            return;
        }
        if (_invokationLog.ContainsKey(name)){
            return;
        }
        _invokationLog.Add(name, 0);
    }

    private void UpdateObjectIntegrityState(){
        if (_synced){
            return;
        }
        var alter = this.GetDbRepresentation();
        if (alter == null){
            if (CheckErrorsExist() || !ValidationProperlyInvoked){
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

    public IReadOnlyCollection<ValidationError> GetIntegriryErrors()
    {
        return _errors.Where(x => x.GetType() == typeof(DbIntegrityValidationError)).ToList().AsReadOnly();
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