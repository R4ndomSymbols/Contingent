namespace StudentTracking.SQL;

public class WhereCondition : IQueryPart{

    private Column _columnConstrained;
    private SQLParameter _value;
    private Relations _relation; 
    public WhereCondition(
        Column constrainedColumn, 
        SQLParameter value,
        Relations relation){
        _columnConstrained = constrainedColumn;
        _value = value;
        _relation = relation;
        _strategy = () => {
            return _columnConstrained.AsSQLText() + " " + _sqlRelations[_relation] + " " + _value.GetName(); 
        };
    }
    // ограничение на NULL
    public WhereCondition(
        Column constrainedColumn, 
        Relations relation){
        _columnConstrained = constrainedColumn;
        _relation = relation;
        _strategy = () => {
            return _columnConstrained.AsSQLText() + " " + _sqlRelations[_relation] + " NULL"; 
        };
    }

    private Func<string> _strategy;
    public string AsSQLText()
    {
        return _strategy.Invoke();
    }

    public enum Relations {
        Equal = 1,
        NotEqual = 2,
        Is = 3,
        IsNot = 4,
        Like = 5,
        In = 6,
        InArray = 7
    }

    private Dictionary<Relations, string> _sqlRelations = new Dictionary<Relations, string>(){
        {Relations.Equal, "="},
        {Relations.NotEqual, "!="},
        {Relations.Is, "IS"},
        {Relations.IsNot, "IS NOT"},
        {Relations.Like, "LIKE"},
        {Relations.In, "IN"},
        {Relations.InArray, "= ANY"}
    };
}