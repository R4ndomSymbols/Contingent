using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using StudentTracking.SQL;
namespace StudentTracking.Statistics;

public class ConstrainedRowHeaderCell : IContsraintTree
{

    // непосредственный родитель
    private ConstrainedRowHeaderCell? _parent;
    // прямые потомки (находнятся на уровень ниже)
    private List<ConstrainedRowHeaderCell> _children;
    private ComplexWhereCondition.ConditionRelation _relationToParent;
    private SQLParameterCollection _parameters; 
    public ComplexWhereCondition? Constraint { get; private set; }
    public SQLParameterCollection TreeParameters => _parameters;
    public string Name { get; set; }
    public int Y { get; set; }
    public int X { get; set; }
    public bool IsOnlyHeader { get; private init; }
    public bool IsRoot { get; private init; }
    public bool HasAnyChildren => _children.Any();
    public CellPlacement Placement { get; set; }
    public IReadOnlyCollection<ConstrainedRowHeaderCell> Children => _children.AsReadOnly();
    // действительная клетка
    public ConstrainedRowHeaderCell(string name, ComplexWhereCondition.ConditionRelation toParent, ConstrainedRowHeaderCell parent) : this(parent)
    {
        IsRoot = false;
        Name = name;
        IsOnlyHeader = false;
    }
    // заголовок
    public ConstrainedRowHeaderCell(string name, ConstrainedRowHeaderCell parent) : this(parent)
    {
        IsRoot = false;
        Constraint = null;
        Name = name;
        IsOnlyHeader = true;
    }
    // корень
    public ConstrainedRowHeaderCell(){
        IsRoot = true;
        _parameters = new SQLParameterCollection();
        _children = new List<ConstrainedRowHeaderCell>();
    
    }

    private ConstrainedRowHeaderCell(ConstrainedRowHeaderCell parent){
        _parent = parent;
        _parent.AddChild(_parent);
        _children = new List<ConstrainedRowHeaderCell>();
        _parameters = parent._parameters;
    } 

    
   

    private ConstrainedRowHeaderCell(ConstrainedRowHeaderCell? parent, IEnumerable<ConstrainedRowHeaderCell>? children)
    {
        _children = new List<ConstrainedRowHeaderCell>();
        if (children is not null)
        {
            _children.AddRange(children);
        }
        _parent = parent;
    }

    private void AddChild(ConstrainedRowHeaderCell child)
    {
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

public interface IContsraintTree {
    public ComplexWhereCondition? GetTreeCondition();
} 


