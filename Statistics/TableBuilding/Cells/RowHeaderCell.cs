using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using StudentTracking.SQL;
namespace StudentTracking.Statistics;

public class RowHeaderCell<T> {

    // непосредственный родитель
    private RowHeaderCell<T>? _parent;
    // прямые потомки (находнятся на уровень ниже)
    private List<RowHeaderCell<T>> _children;

    public Filter<T> NodeFilter {get; private set;}
    public string Name { get; private set;}
    public bool IsOnlyStructural { get; private init; }
    public bool IsRoot => _parent is null;
    public bool HasAnyChildren => _children.Any();
    public CellPlacement Placement { get; set; }
    public IReadOnlyCollection<RowHeaderCell<T>> Children => _children.AsReadOnly();
    // клетка
    public RowHeaderCell(string name, RowHeaderCell<T> parent, Filter<T>? nodeFilter = null)
    {
        if (parent is null){
            throw new Exception("Родитель горизонтального заголовка не может быть пустой"); 
        }
        Name = name;
        _parent = parent;
        _parent.AddChild(this);
        _children = new List<RowHeaderCell<T>>();
        if (nodeFilter is null){
            IsOnlyStructural = true;
            NodeFilter = Filter<T>.Empty;
        }
        else {
            IsOnlyStructural = false;
            NodeFilter = nodeFilter.Include(_parent.NodeFilter); 
        }
    }

    // корень
    public RowHeaderCell(){
        _parent = null;
        NodeFilter = Filter<T>.Empty;
        _children = new List<RowHeaderCell<T>>();
    
    }

    private void AddChild(RowHeaderCell<T> child)
    {
        child._parent = this;
        _children.Add(child);
        
    } 
}

public interface IContsraintTree {
    public ComplexWhereCondition? GetTreeCondition();
} 


