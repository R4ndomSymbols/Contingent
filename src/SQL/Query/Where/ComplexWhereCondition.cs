namespace Contingent.SQL;

public class ComplexWhereCondition : IQueryPart
{

    private WhereCondition? _facade;
    private ComplexWhereCondition? _left;
    private ComplexWhereCondition? _right;
    private ConditionRelation _relation;
    private bool _isGroup;
    private Func<string> _strategy;
    private bool _isTreeHead;
    private bool _isEmpty;

    public bool IsEmpty => _isEmpty;

    public static ComplexWhereCondition Empty => new ComplexWhereCondition();

    private ComplexWhereCondition()
    {
        _isEmpty = true;
        _isTreeHead = false;
        _strategy = () => "";
    }


    public enum ConditionRelation
    {
        AND = 1,
        OR = 2,
    }
    public ComplexWhereCondition(WhereCondition single)
    {
        _facade = single;
        _strategy = () => _facade.AsSQLText();
        _isTreeHead = true;
        _isEmpty = false;
    }

    public ComplexWhereCondition(WhereCondition left, WhereCondition right, ConditionRelation relation, bool isGroup = false)
    : this(new ComplexWhereCondition(left), new ComplexWhereCondition(right), relation, isGroup)
    {

    }

    public ComplexWhereCondition(ComplexWhereCondition left, ComplexWhereCondition right, ConditionRelation relation, bool isGroup = false)
    {
        if (left._isEmpty || right._isEmpty)
        {
            throw new Exception("Нельзя использовать пустой фильтр в конструкторе");
        }
        _isGroup = isGroup;
        _left = left;
        _right = right;
        _relation = relation;
        _isTreeHead = true;
        SuppressChildWhere();
        _strategy = () =>
        {
            var result = " " + _left.AsSQLText() + " " + _relation.ToString() + " " + _right.AsSQLText() + " ";
            if (_isGroup)
            {
                result = "( " + result + ")";
            }
            return result;
        };
        _isEmpty = false;
    }

    public ComplexWhereCondition Unite(ConditionRelation op, ComplexWhereCondition? next, bool endGroup = false)
    {
        if (_isEmpty)
        {
            if (next is null)
            {
                return this;
            }
            return next;
        }
        if (next is null || next._isEmpty)
        {
            return this;
        }
        return new ComplexWhereCondition(this, next, op, endGroup);
    }

    public IEnumerable<Column> GetAllRestrictedColumns()
    {
        var found = new List<Column>();
        TraceDown(this, found);
        return found;

        void TraceDown(ComplexWhereCondition condition, List<Column> history)
        {
            if (condition._left is not null)
            {
                TraceDown(condition._left, history);
            }
            if (condition._right is not null)
            {
                TraceDown(condition._right, history);
            }
            if (condition._facade is not null)
            {
                history.Add(condition._facade.RestrictedLeft);
            }
        }

    }
    private void SuppressChildWhere()
    {
        if (_left is not null)
        {
            _left._isTreeHead = false;
            _left.SuppressChildWhere();
        }
        if (_right is not null)
        {
            _right._isTreeHead = false;
            _right.SuppressChildWhere();
        }
    }


    public string AsSQLText()
    {
        return (_isTreeHead ? "WHERE " : " ") + _strategy.Invoke();
    }

}

