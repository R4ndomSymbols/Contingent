
namespace Utilities;

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

public abstract class InDbValidatedObject : ValidatedObject{
    
    private RelationTypes _dbRelation;
    protected int _id;
    private bool _synced;
    protected RelationTypes ObjectState {
        get {
            if (_synced){
                return _dbRelation;
            }
            else{
                UpdateObjectIntegrityState();
                return _dbRelation;
            }
        }
    }

    public RelationTypes CurrentState {
        get => _dbRelation;
    }
    protected bool PerformValidation(Func<bool> validation, DbIntegrityValidationError err)
    {
        _synced = false;
        return base.PerformValidation(validation, err);
    }
    public bool CheckIntegrityErrorsExist(){
        return _validationErrors.Any(x => x.GetType() == typeof(DbIntegrityValidationError));
    }
    public static virtual InDbValidatedObject? GetById(int id);

    private void UpdateObjectIntegrityState(){
        var alter = GetById(_id);
        if (alter == null){
            if (CheckIntegrityErrorsExist()){
                _dbRelation = RelationTypes.UnboundInvalid;
            }
            else{
                _dbRelation = RelationTypes.Pending;
            }
        } 
        else if (alter.Equals(this)){
            _dbRelation = RelationTypes.Bound;
        }
        else{
            if(CheckIntegrityErrorsExist()){
                _dbRelation = RelationTypes.Invalid;
            }
            else{
                _dbRelation = RelationTypes.Modified;
            }
        }
    }        
}
