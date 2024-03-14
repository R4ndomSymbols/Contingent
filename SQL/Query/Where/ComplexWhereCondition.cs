namespace StudentTracking.SQL;

public class ComplexWhereCondition : IQueryPart{
    
    private WhereCondition? _facaded;
    private ComplexWhereCondition? _left;
    private ComplexWhereCondition? _right;
    private ConditionRelation _relation;
    private bool _isGroup;
    private Func<string> _strategy;
    private bool _isTreeHead;

    public enum  ConditionRelation {
        AND = 1,
        OR = 2,
    }
    public ComplexWhereCondition(WhereCondition single) {
        _facaded = single;
        _strategy = () => _facaded.AsSQLText();
        _isTreeHead = true;
    }

    public ComplexWhereCondition(WhereCondition left, WhereCondition right, ConditionRelation relation, bool isGroup = false)
    : this(new ComplexWhereCondition(left), new ComplexWhereCondition(right), relation, isGroup)
    {
         
    }

    public ComplexWhereCondition(ComplexWhereCondition left, ComplexWhereCondition right, ConditionRelation relation, bool isGroup = false){
        _isGroup = isGroup;
        _left = left;
        _right = right;
        _relation = relation;
        _isTreeHead = true;
        SuppressChildWhere();
        _strategy = () => {
            var result = " " + _left.AsSQLText() + " "  + _relation.ToString() + " " + _right.AsSQLText() + " ";
            if (_isGroup){
                result = "( " + result + ")";
            }
            return result;
        };
    }

    public static ComplexWhereCondition? Unite(ConditionRelation op, IEnumerable<ComplexWhereCondition?>? toUnite){
        if (toUnite is null){
            return null;
        }
        var filtered = toUnite.Where(x => x is not null);
        if (!filtered.Any()){
            return null;
        }
        var head = toUnite.First();
        foreach(var next in toUnite.Skip(1)){
            head = new ComplexWhereCondition(head, next, op);
        }
        return head;
    }

    public IEnumerable<Column> GetAllRestrictedColumns(){
        var found = new List<Column>();
        TraceDown(this, found);
        return found;

        void TraceDown(ComplexWhereCondition condition, List<Column> history){
            if (condition._left is not null){
                TraceDown(condition._left, history);
            }
            if (condition._right is not null){
                TraceDown(condition._right, history);
            }
            if (condition._facaded is not null){
                history.Add(condition._facaded.RestrictedLeft);
            }
        }

    } 
    private void SuppressChildWhere(){
        if (_left is not null){
            _left._isTreeHead = false;
            _left.SuppressChildWhere();
        }
        if (_right is not null){
            _right._isTreeHead = false;
            _right.SuppressChildWhere();
        }
    }


    public string AsSQLText()
    {
        return  (_isTreeHead ? "WHERE " : " ") + _strategy.Invoke();
    }

}

