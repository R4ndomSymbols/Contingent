using System.Drawing;
using System.Net;

namespace StudentTracking.Statistics;

// добавить нумерацию колонок и столбцов
public class TableColumnHeader {

    private ConstrainedColumnHeaderCell _root;
    private bool _isNumerationUsed;
    public int HeaderLength {get; private set;}
    public int HeaderHeigth {get; private set;}

    public TableColumnHeader(ConstrainedColumnHeaderCell root, bool addNumeration){
        _root = root;
        _isNumerationUsed = addNumeration;
        Normalize();
    }

    private void Normalize(){
        if (!_root.IsRoot || !_root.Children.Any()){
            throw new Exception("Нормализация неосуществима вне корневой или пустой ноды");
        }
        var cursor = new HeaderBuilderCursor();
        Normalize(_root, cursor);
        // курсор проходит по графу и запоминает предельные значения
        HeaderLength = cursor.MaxXOffser + 1;
        HeaderHeigth = cursor.MaxDepth + 1;
        ExtendToRectange(_root, cursor);
        if (_isNumerationUsed){
            int num = 1;
            AddNumeration(_root, ref num);
            // корректировка на строку нумерации
            HeaderHeigth += 1;
        }
    }
    // нормализует таблицу по ширине
    private void Normalize(ConstrainedColumnHeaderCell root, HeaderBuilderCursor cursor){
        if (root.Children.Any()){
            var colSpanSummary = 0;
            var xAnchor = cursor.X; 
            foreach (var child in root.Children){
                cursor.Y+=1;
                Normalize(child, cursor);
                cursor.Y-=1;
                cursor.X+=child.Placement.ColumnSpan;
                colSpanSummary+=child.Placement.ColumnSpan;
            }
            root.Placement = new CellPlacement(xAnchor, cursor.Y, 1, colSpanSummary);
            cursor.X = xAnchor;

        }
        else{
            root.Placement = new CellPlacement(cursor.X, cursor.Y, 1,1);
        }
    }
    // курсор уже должен быть заполнен после нормализации
    // метод приводит заголовок в прямоугольный вид
    // нормализует таблицу по высоте
    private void ExtendToRectange(ConstrainedColumnHeaderCell root, HeaderBuilderCursor cursor){
        if (root.Children.Any()){
            foreach (var child in root.Children){
                cursor.Y+=1;
                ExtendToRectange(child, cursor);
                cursor.Y-=1;
            }
        }
        else {
            root.Placement = root.Placement.ChangeSize(rows: root.Placement.RowSpan + (cursor.MaxDepth - cursor.Y));
        }
    }
    // вызывается после всех методов и добавляет нумерацию к столбцам
    private void AddNumeration(ConstrainedColumnHeaderCell root, ref int startNumber){
        if (root.Children.Any()){
            foreach(var child in root.Children){
                AddNumeration(child, ref startNumber);
            }
        }
        else {
            var numericColumn = new ConstrainedColumnHeaderCell(startNumber.ToString(), root);
            root.AddChild(numericColumn);
            numericColumn.Placement = new CellPlacement(x: root.Placement.X, root.Placement.Y, 1, 1);
            startNumber+=1;
        }
    }
    // выполняет делегат по дереву (дерево предполагается полностью инициализированным)
    private void TraceTree(Action<ConstrainedColumnHeaderCell> toPerform, ConstrainedColumnHeaderCell start){
        if (start.HasAnyChildren){
            foreach (var cell in start.Children){
                TraceTree(toPerform, cell);
            }
        }
        else{
            toPerform.Invoke(start);
        }
    }
    // координата x абсолютная и начинается с самой крайней левой ячейки
    public IEnumerable<ConstrainedColumnHeaderCell> TraceVertical(int x){
        var found = new List<ConstrainedColumnHeaderCell>();
        Action<ConstrainedColumnHeaderCell> cellGetter = 
        (cell) => {
            if (cell.Constraint is not null && 
                cell.Placement.X >= x && cell.Placement.ColumnSpan - 1 + cell.Placement.X <= x){
                found.Add(cell);
            }
        };
        TraceTree(cellGetter, _root);
        return found;

    }

    
}

public class HeaderBuilderCursor{

    private int _y;
    private int _x;
    private int _maxY;
    private int _maxX;
    public int X {get => _x; 
        set {
            if (value > _maxX){
                _maxX = value;
            }
            _x = value;
        }
    }
    public int Y {
        get => _y;
        set {
            if (value > _maxY){
                _maxY = value;
            }
            _y = value;
        }
    }
    public int MaxDepth {get => _maxY;}
    public int MaxXOffser {get => _maxX;}

    public HeaderBuilderCursor(){
        _y = 0;
        _x = 0;
    }

    

    
}




