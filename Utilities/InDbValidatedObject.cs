
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

public abstract class InDbValidatedObject<T> : ValidatedObject<T>{
    
    protected RelationTypes _dbRelation = RelationTypes.UnboundInvalid;

    public RelationTypes CurrentState {
        get => _dbRelation;
    }
    protected bool PerformValidation(Func<bool> validation, DbIntegrityValidationError<T> err) {
        return base.PerformValidation(validation, err);
    }
    public bool CheckIntegrityErrorsExist(){
        return _validationErrors.Any(x => x.GetType() == typeof(DbIntegrityValidationError<T>));
    }

    protected abstract void ValidateDbIntegrity();     
}
