namespace StudentTracking.Models.SQL;

public class WhereCondition {

    private SQLParameters _parameters;
    private Column _columnConstrained;
    private SQLParameter _value;
    private Relations _relation; 
    private readonly string _name;
    public WhereCondition(SQLParameters sqlP,
        Column constrainedColumn, 
        SQLParameter value,
        Relations relation){
        _parameters = sqlP;
        _columnConstrained = constrainedColumn;
        _value = value;
        _relation = relation;
        _name = _parameters.Add(_value);

        _strategy = () => {
            return _columnConstrained.ToString() + " " + _sqlRelations[_relation] + " " + _name; 
        };
    }
    // ограничение на NULL
    public WhereCondition(
        Column constrainedColumn, 
        Relations relation){
        _columnConstrained = constrainedColumn;
        _relation = relation;
        _strategy = () => {
            return _columnConstrained.ToString() + " " + _sqlRelations[_relation] + " NULL"; 
        };
    }

    private Func<string> _strategy;

    public override string ToString()
    { 
        return _strategy.Invoke();
    }

    public enum Relations {
        Equal = 1,
        NotEqual = 2,
        Is = 3,
        IsNot = 4,
        Like = 5,
    }

    private Dictionary<Relations, string> _sqlRelations = new Dictionary<Relations, string>(){
        {Relations.Equal, "="},
        {Relations.NotEqual, "!="},
        {Relations.Is, "IS"},
        {Relations.IsNot, "IS NOT"},
        {Relations.Like, "LIKE"}
    };
}