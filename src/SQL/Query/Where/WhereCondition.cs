namespace StudentTracking.SQL;

public class WhereCondition : IQueryPart{

    private Column _columnLeft;
    private Column? _columnRight;
    public Column RestrictedLeft => _columnLeft;
    public Column? RestrictedRight => _columnRight;
    private SQLParameter? _value;
    private Relations _relation; 
    public WhereCondition(
        Column constrainedColumn, 
        SQLParameter value,
        Relations relation){
        _columnLeft = constrainedColumn;
        _value = value;
        _relation = relation;
        _strategy = () => {
            return _columnLeft.AsSQLText() + " " + _sqlRelations[_relation] + " " + _value.GetName(); 
        };
    }
    public WhereCondition(
        Column leftColumn, 
        Column rigthColumn,
        Relations relation){
        _columnLeft = leftColumn;
        _value = null;
        _relation = relation;
        _columnRight = rigthColumn;
        _strategy = () => {
            return _columnLeft.AsSQLText() + " " + _sqlRelations[_relation] + " " + _columnRight.AsSQLText(); 
        };
    }
    // ограничение на NULL
    public WhereCondition(
        Column constrainedColumn, 
        Relations relation){
        _columnLeft = constrainedColumn;
        _relation = relation;
        _strategy = () => {
            return _columnLeft.AsSQLText() + " " + _sqlRelations[_relation] + " NULL"; 
        };
    }
    public WhereCondition(
        Column constrainedColumn,
        SelectQuery query, 
        Relations relation){
        _columnLeft = constrainedColumn;
        _relation = relation;
        _strategy = () => {
            return _columnLeft.AsSQLText() + " " + _sqlRelations[_relation] + " ( " + query.AsSQLText() + " ) "; 
        };
    }
    public WhereCondition(
        Column constrainedColumn, 
        string parameterValue,
        Relations relation){
        _columnLeft = constrainedColumn;
        _relation = relation;
        _strategy = () => {
            return _columnLeft.AsSQLText() + " " + _sqlRelations[_relation] + " " + parameterValue; 
        };
    }
    public WhereCondition(
        Column constrainedBoolColumn){
        _columnLeft = constrainedBoolColumn;
        _strategy = () => {
            return _columnLeft.AsSQLText(); 
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
        InArray = 7,
        Less = 8,
        Bigger = 9,
        LessOrEqual = 10,
        BiggerOrEqual = 11
    }

    private Dictionary<Relations, string> _sqlRelations = new Dictionary<Relations, string>(){
        {Relations.Equal, "="},
        {Relations.NotEqual, "!="},
        {Relations.Is, "IS"},
        {Relations.IsNot, "IS NOT"},
        {Relations.Like, "LIKE"},
        {Relations.In, "IN"},
        {Relations.InArray, "= ANY"},
        {Relations.Less, "<"},
        {Relations.Bigger, ">"},
        {Relations.LessOrEqual, "<="},
        {Relations.BiggerOrEqual, ">="}
    };
}