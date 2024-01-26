
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using Npgsql;
using StudentTracking.Models.Domain;

namespace Utilities.Validation;


public class DbValidatedObject : IDbObjectValidated
{
    private List<ValidationError>? _errors;
    private Dictionary<string, int>? _invokationLog;
    private bool _synced;
    private RelationTypes _relationBetweenObjAndDB;

    public void PrintLog(){
        Console.WriteLine(string.Join("\n", _invokationLog.Select(x => x.Key + " " + x.Value)));
    }
    private bool ValidationProperlyInvoked {
        
        get {
            if (_invokationLog == null){
                throw new InvalidOperationException("Смена состояний неинициализированной валидации невозможна, свойство");
            }
            return _invokationLog.All(x => x.Value > 0);
        } 
    }

    public bool CheckPropertyValidity(string propertyName){
        if (_invokationLog == null || _errors == null){
            throw new InvalidOperationException("Валидация оключена");
        }
        if (!_invokationLog.ContainsKey(propertyName)){
            throw new ArgumentException("Такое поле не зарегитрировано");
        }
        return _invokationLog[propertyName] > 0 && !_errors.Any(x => x.PropertyName == propertyName);
    }

    public async Task<RelationTypes> GetCurrentState(ObservableTransaction? scope){
        await UpdateObjectIntegrityState(scope);
        return _relationBetweenObjAndDB;
    }

    protected DbValidatedObject(){
        _errors = new List<ValidationError>();
        _invokationLog = new Dictionary<string, int>();
        _synced = false;
        _relationBetweenObjAndDB = RelationTypes.UnboundInvalid;
    }

    protected DbValidatedObject(RelationTypes initRelation){
        _errors = new List<ValidationError>();
        _relationBetweenObjAndDB = initRelation;
        switch (initRelation)
        {
            case RelationTypes.UnboundInvalid:
            case RelationTypes.Invalid:
                _errors = new List<ValidationError>();
                _invokationLog = new Dictionary<string, int>();
                _synced = false;
                break;
            case RelationTypes.Bound:
                _errors = null;
                _invokationLog = null;
                _synced = true;
                break;
        }
    }

    public bool CheckErrorsExist()
    {
        if (_errors != null){
            return _errors.Any();          
        }
        else{
            return false;
        }
    }

    public bool CheckIntegrityErrorsExist()
    {
        if (_errors != null){
            return _errors.Any(x => x.GetType() == typeof(DbIntegrityValidationError));
        }
        return false;
    }

    public IReadOnlyCollection<ValidationError> GetErrors()
    {
        if (_errors == null){
            return new List<ValidationError>().AsReadOnly();
        }
        return _errors.ToList().AsReadOnly();
    }

    protected void AddError(ValidationError err){
        if (_errors == null){
            return;
        }
        _errors.Add(err);
    }
    private void ClearState(string propName){
        if (_errors == null){
            return;
        }
        _errors = _errors.Where(x => x.PropertyName != propName).ToList();
    }
    public bool PerformValidation(Func<bool> validation, ValidationError err){
        if (_errors == null){
            _errors = new List<ValidationError>();
            _relationBetweenObjAndDB = RelationTypes.UnboundInvalid;
        }

        ClearState(err.PropertyName);
        bool validationResult = validation.Invoke();
        try {
            if (_invokationLog == null){
                throw new ArgumentNullException("непредвиденная ошибка инициализации словаря валидации");
            }
            _invokationLog[err.PropertyName] = _invokationLog[err.PropertyName] + 1;
        }
        catch (KeyNotFoundException){
            throw new ArgumentException("Поле не зарегистрировано");
        }
        
        if (!validationResult){
            AddError(err);
        }
        _synced = false;
        return validationResult;
    }
    
    protected void RegisterProperty(string? name){
        if (_invokationLog == null){
            _invokationLog = new Dictionary<string, int>();
        }

        if (name == null){
            return;
        }
        if (_invokationLog.ContainsKey(name)){
            return;
        }
        _invokationLog.Add(name, 0);
    }

    public void RegisterProperty(ValidationError? initial){
        if (_invokationLog == null){
            throw new InvalidOperationException("Валидация объекта отключена");
        }
        if (initial is null){
            throw new ArgumentNullException("Ошибка должна быть указана");
        }
        if (_invokationLog.ContainsKey(initial.PropertyName)){
            return;
        }
        _invokationLog.Add(initial.PropertyName, 0);
        if (_errors == null){
            throw new InvalidOperationException("Валидация объекта отключена");
        }
        _errors.Add(initial);
    }


    private async Task UpdateObjectIntegrityState(ObservableTransaction? scope){
        if (_invokationLog == null){
            throw new InvalidOperationException("Смена состояний неинициализированной валидации невозможна");
        }
        if (_synced){
            return;
        }

        //Console.WriteLine(this.GetType().ToString() + " " + ValidationProperlyInvoked.ToString() + " " + CheckErrorsExist().ToString());
        //Console.WriteLine(string.Join("\n", _invokationLog.Select(x => x.Key + " " + x.Value.ToString())));
        var alter = await GetDbRepresentation(scope);
        
        if (alter != null && alter.GetType() == typeof(RussianCitizenship)){
            var tmp1 = (RussianCitizenship)alter;
            Console.WriteLine("alter " + tmp1.Id + " " + tmp1.PassportNumber + " " + tmp1.PassportSeries);
        }

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
        if (_errors == null){
            return new List<ValidationError>().AsReadOnly();
        }
        return _errors.Where(x => x.GetType() == typeof(DbIntegrityValidationError)).ToList().AsReadOnly();
    }

    public virtual bool Equals(IDbObjectValidated? other)
    {
        throw new NotImplementedException("Метод сравнения не переопределен");
    }

    public virtual async Task<IDbObjectValidated?> GetDbRepresentation(ObservableTransaction? scope)
    {   
        await Task.Delay(10);
        throw new NotImplementedException("Метод получения сущности БД не переопределен");
    }

    protected void NotifyStateChanged(){
        _synced = false;
    }

    public void WriteErrors(){
        if (_errors != null){
            Console.WriteLine(string.Join("\n", _errors));
        }
    }

    public IReadOnlyCollection<ValidationError> FilterErrorsByName(string name)
    {
        if (_errors == null){
            return new List<ValidationError>();
        }
        else{
            return _errors.Where(x => x.PropertyName == name).ToList();
        }
    }

    public void PrintValidationLog(){
        if (_invokationLog!=null){
            Console.WriteLine(
                string.Join("\n", _invokationLog.Select(x => "Свойство: " + x.Key + " К: " + x.Value))
            );
        }
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