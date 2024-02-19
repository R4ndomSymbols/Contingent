using StudentTracking.SQL;
namespace StudentTracking.Statistics;

public class ConstrainedRowHeaderCell {
    
    // непосредственный родитель
    private ConstrainedRowHeaderCell? _parent;
    // прямые потомки (находнятся на уровень ниже)
    private List<ConstrainedRowHeaderCell> _children;
    public ComplexWhereCondition? Constraint {get; set;}
    public string Name {get; set; }
    public int Y {get; set; }
    public int X {get; set; } 
    public bool IsOnlyHeader {get; private init ;}
    public bool IsRoot {get; private init; }
    public bool HasAnyChildren => _children.Any();
    public CellPlacement Placement {get; set;}
    public IReadOnlyCollection<ConstrainedRowHeaderCell> Children => _children.AsReadOnly();
    // действительная клетка
    public ConstrainedRowHeaderCell(string name, ComplexWhereCondition constraint,  ConstrainedRowHeaderCell? parent = null, IEnumerable<ConstrainedRowHeaderCell>? children = null) :
    this(parent, children)
    {
        Constraint = constraint;
        Name = name;
        IsOnlyHeader = false;
    }
    // заголовок
    public ConstrainedRowHeaderCell(string name, ConstrainedRowHeaderCell? parent = null, IEnumerable<ConstrainedRowHeaderCell>? children = null) 
    : this(parent, children)
    {
        Constraint = null;
        Name = name; 
        IsOnlyHeader = true;
    }

    private ConstrainedRowHeaderCell(ConstrainedRowHeaderCell? parent, IEnumerable<ConstrainedRowHeaderCell>? children){
        _children = new List<ConstrainedRowHeaderCell>();
        if (children is not null){
            _children.AddRange(children);
        }
        _parent = parent;
    }
   

    public void Link(ConstrainedRowHeaderCell aggregateParticipant){
        _toAggregate.Add(aggregateParticipant);
    }
}

