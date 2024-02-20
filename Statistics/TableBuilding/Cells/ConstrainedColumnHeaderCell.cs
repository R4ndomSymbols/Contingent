using System.Drawing;
using StudentTracking.SQL;
namespace StudentTracking.Statistics;

public class ConstrainedColumnHeaderCell<T> : IContsraintTree {

    private ConstrainedColumnHeaderCell<T>? _parent;
    private List<ConstrainedColumnHeaderCell<T> _children;
    public string Name {get; set; }
    public CellPlacement Placement {get; set;} 
    public IReadOnlyList<ConstrainedColumnHeaderCell> Children => _children;
    public bool HasAnyChildren => _children.Any();
    public bool IsRoot {get; private set; } 
    // колонка с фильтром
    public ConstrainedColumnHeaderCell(string name, ConstrainedColumnHeaderCell parent) : this(name, parent){
        Constraint = constraint;
        _relationToParent = relation;
    }
    // корень
    public ConstrainedColumnHeaderCell(SQLParameterCollection parameters){
        Constraint = null;
        Name = string.Empty;
        _parent = null;
        IsRoot = true;
        _children = new List<ConstrainedColumnHeaderCell>();
        _parameters = parameters;
    }
    // дочернаяя колонка
    public ConstrainedColumnHeaderCell(string name, ConstrainedColumnHeaderCell parent){
        IsRoot = false;
        Name = name;
        _parent = parent;
        _parameters = parent._parameters;
        _parent.AddChild(this);
        _children = new List<ConstrainedColumnHeaderCell>();
    }

    // конструируется дерево, потом оно будет выравнено до прямоугольника
    private void AddChild(ConstrainedColumnHeaderCell child){
        child._parent = this;
        _children.Add(child);
    }

    public ComplexWhereCondition? GetTreeCondition()
    {
        if (_parent is null)
        {
            return Constraint;
        }
        var fromParent = GetTreeCondition();
        if (fromParent is null)
        {
            return Constraint;
        }
        else
        {
            if (Constraint is null)
            {
                return fromParent;
            }
            else
            {
                return new ComplexWhereCondition(Constraint, fromParent, _relationToParent);
            }
        }
    }
    
}

