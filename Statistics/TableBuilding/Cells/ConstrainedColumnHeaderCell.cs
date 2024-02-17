using System.Drawing;
using StudentTracking.SQL;
namespace StudentTracking.Statistics;

public class ConstrainedColumnHeaderCell {

    private ConstrainedColumnHeaderCell? _parent;
    private List<ConstrainedColumnHeaderCell> _children;
    private bool _isRoot;
    public ComplexWhereCondition? Constraint {get; set;}
    public string Name {get; set; }
    public CellPlacement Placement {get; set;} 
    public IReadOnlyList<ConstrainedColumnHeaderCell> Children => _children;

    public bool IsRoot => _isRoot; 
    // колонка с фильтром
    public ConstrainedColumnHeaderCell(string name, ConstrainedColumnHeaderCell parent, ComplexWhereCondition constraint){
        Constraint = constraint;
        Name = name;
        _children = new List<ConstrainedColumnHeaderCell>();
        _isRoot = false;
        _parent = parent;
    }
    // колонка с названием без фильтра
    public ConstrainedColumnHeaderCell(string name, ConstrainedColumnHeaderCell parent){
        Constraint = null;
        Name = name;
        _children = new List<ConstrainedColumnHeaderCell>();
        _isRoot = false;
        _parent = parent;
    }
    // корень
    public ConstrainedColumnHeaderCell(){
        Constraint = null;
        Name = string.Empty;
        _parent = null;
        _isRoot = true;
        _children = new List<ConstrainedColumnHeaderCell>();
    }
    // конструируется дерево, потом оно будет выравнено до прямоугольника
    public void AddChild(ConstrainedColumnHeaderCell child){
        _children.Add(child);
    }
}

