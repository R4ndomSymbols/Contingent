using System.Drawing;
using StudentTracking.SQL;
namespace StudentTracking.Statistics;

// параметр нужен для фильтрации
public class ColumnHeaderCell<T> {

    private ColumnHeaderCell<T>? _parent;
    private List<ColumnHeaderCell<T>> _children;
    private Filter<T> _nodeFilter;
    public string Name {get; set; }
    public CellPlacement Placement {get; set;} 
    public IReadOnlyList<ColumnHeaderCell<T>> Children => _children;
    public bool HasAnyChildren => _children.Any();
    public bool IsRoot => _parent is null;
    public bool IsOnlyStructural => _nodeFilter is null; 
    // колонка с фильтром
    public ColumnHeaderCell(string name, ColumnHeaderCell<T> parent, Filter<T>? nodeFilter = null) : this(name, parent){
        if (nodeFilter is not null){
            _nodeFilter = nodeFilter;
        }
        _nodeFilter = Filter<T>.Empty;
    }
    // корень
    public ColumnHeaderCell(){
        Name = string.Empty;
        _parent = null;
        _children = new List<ColumnHeaderCell<T>>();
    }
    // дочернаяя колонка
    private ColumnHeaderCell(string name, ColumnHeaderCell<T> parent){
        if (parent is null){
            throw new Exception("Родитель дочерней ноды должен быть указан");
        }
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

    public Filter<T> GetFilterSequence(){
        if (IsRoot){
            return Filter<T>.Empty;
        }
        if (IsOnlyStructural){
            return _parent.GetFilterSequence();
        }
        else{
            return _parent._nodeFilter.Merge(_nodeFilter);
        }

    }    
}

