using Npgsql.Replication.PgOutput.Messages;

namespace StudentTracking.Models.SQL;

public class ComplexWhereCondition : IQueryPart{
  
    private WhereCondition? _left;
    private WhereCondition? _right;
    private ConditionRelation _relation;
    private bool _isGroup; 

    public enum  ConditionRelation {
        AND = 1,
        OR = 2,
    }

    public ComplexWhereCondition(WhereCondition left, WhereCondition right, ConditionRelation relation, bool isGroup){
        _left = left;
        _right = right;
        _relation = relation;
        _isGroup = isGroup;

        _strategy = () => {
            var result = " " + _left.AsSQLText() + " "  + _relation.ToString() + " " + _right.AsSQLText() + " ";
            if (_isGroup){
                result = "( " + result + ")";
            }
            return result;
        };
    }

    public ComplexWhereCondition(WhereCondition single) {
        _left = single;
        _strategy = () => _left.AsSQLText();
    }

    private Func<string> _strategy;

    public string AsSQLText()
    {
        return "WHERE " + _strategy.Invoke();
    }
}

