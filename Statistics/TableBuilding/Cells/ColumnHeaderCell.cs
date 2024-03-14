namespace StudentTracking.Statistics;

// параметр нужен для фильтрации
public class ColumnHeaderCell<T> {

    private ColumnHeaderCell<T>? _parent;
    private List<ColumnHeaderCell<T>> _children;
    public Filter<T> NodeFilter {get; private init; }
    private CellPlacement _placement;
    public bool IsFixed {get; private init;}
    public string Name {get; set; }
    public CellPlacement Placement {
        get => _placement;
        set {
            if (IsFixed){
                return;
            }
            _placement = value;
        }
    } 
    public IReadOnlyList<ColumnHeaderCell<T>> Children => _children;
    public bool HasAnyChildren => _children.Any();
    public bool IsRoot => _parent is null;
    public bool IsOnlyStructural => NodeFilter is null; 
    // колонка с фильтром
    public ColumnHeaderCell(string name, ColumnHeaderCell<T> parent, Filter<T>? nodeFilter = null) : this(name, parent, false){
        if (nodeFilter is not null){
            NodeFilter = nodeFilter.Include(parent.NodeFilter);
        }
        else{
            NodeFilter = Filter<T>.Empty;
        }
        
    }
     public ColumnHeaderCell(string name, ColumnHeaderCell<T> parent, CellPlacement permanent, Filter<T>? nodeFilter = null) : this(name, parent, true){
        if (nodeFilter is not null){
            NodeFilter = nodeFilter.Include(parent.NodeFilter);
        }
        else{
            NodeFilter = Filter<T>.Empty;
        }
        _placement = permanent;
    }
    // корень
    public ColumnHeaderCell(){
        Name = string.Empty;
        IsFixed = false;
        _parent = null;
        NodeFilter = Filter<T>.Empty; 
        _children = new List<ColumnHeaderCell<T>>();
    }
    // дочернаяя колонка
    private ColumnHeaderCell(string name, ColumnHeaderCell<T> parent, bool isFixed){
        if (parent is null){
            throw new Exception("Родитель дочерней ноды должен быть указан");
        }
        IsFixed = isFixed;
        Name = name;
        _parent = parent;
        _parent.AddChild(this);
        _children = new List<ColumnHeaderCell<T>>();
    }

    // конструируется дерево, потом оно будет выравнено до прямоугольника
    private void AddChild(ColumnHeaderCell<T> child){
        child._parent = this;
        _children.Add(child);
    }

    public int GetTreeLogicalHeight(){
        // корневая нода не участвует в компоновке
        if (IsRoot){
            return 0;
        }
        else{
            return 1 + _parent.GetTreeLogicalHeight(); 
        }
    }
    public int GetTreeGeometricalHeight(){
        if (IsRoot){
            return 0;
        }
        else{
            return _placement.RowSpan + _parent.GetTreeGeometricalHeight();
        }
    }    
}

