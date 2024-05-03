using System.Text;

namespace Contingent.Statistics;

// добавить нумерацию колонок и столбцов
public class TableColumnHeader<T>
{

    private ColumnHeaderCell<T> _root;
    private bool _isNumerationUsed;
    public int HeaderWidth { get; private set; }
    public int HeaderHeigth { get; private set; }

    public TableColumnHeader(ColumnHeaderCell<T> root, bool addNumeration)
    {
        // корневая нода не должна быть видимой, она задает основные условия для всей таблицы
        // является агрегатом всех остальных
        if (!root.IsRoot || !root.HasAnyChildren)
        {
            throw new Exception("Корневая нода не должна быть некорневой или пустой");
        }
        _root = root;
        _isNumerationUsed = addNumeration;
        CheckCycle(_root, new List<ColumnHeaderCell<T>>());
        Normalize(out HeaderBuilderCursor cursor);

        // дебаг

        Action<ColumnHeaderCell<T>> printCords = (cell) => Console.WriteLine(cell.Placement + " " + cell.Name);
        TraceTree(printCords, _root);

    }
    // не оптимальное решение
    private void CheckCycle(ColumnHeaderCell<T> current, List<ColumnHeaderCell<T>> log)
    {
        if (log.Any(node => object.ReferenceEquals(node, current)))
        {
            throw new Exception("Обнаружен цикл в графе колонок");
        }
        log.Add(current);
        if (current.HasAnyChildren)
        {
            foreach (var child in current.Children)
            {
                CheckCycle(child, log);
            }
        }
    }

    private void Normalize(out HeaderBuilderCursor cursor)
    {
        cursor = new HeaderBuilderCursor();
        // корень имеет отрицательный y
        cursor.Y = -1;
        cursor.X = 0;
        // курсор проходит по графу и запоминает предельные значения
        Normalize(_root, cursor);
        if (_isNumerationUsed)
        {
            int num = 1;
            AddNumeration(_root, ref num);
        }
        ExtendToRectange(_root, cursor);
        HeaderHeigth = cursor.MaxY + 1;
        // Из-за алгоритма работы normalize 
        // происходит одно лишнее смещение
        HeaderWidth = cursor.MaxX;
    }
    // нормализует таблицу по ширине
    private void Normalize(ColumnHeaderCell<T> root, HeaderBuilderCursor cursor)
    {
        if (root.HasAnyChildren)
        {
            var colSpanSummary = 0;
            var xAnchor = cursor.X;
            foreach (var child in root.Children)
            {
                cursor.Y += 1;
                Normalize(child, cursor);
                cursor.Y -= 1;
                colSpanSummary += child.Placement.ColumnSpan;
            }
            root.Placement = new CellPlacement(xAnchor, cursor.Y, 1, colSpanSummary);
        }
        else
        {
            root.Placement = new CellPlacement(cursor.X, cursor.Y, 1, 1);
            cursor.X++;
        }
    }
    // курсор уже должен быть заполнен после нормализации
    // метод приводит заголовок в прямоугольный вид
    // нормализует таблицу по высоте
    private void ExtendToRectange(ColumnHeaderCell<T> startCell, HeaderBuilderCursor cursor)
    {
        if (startCell.HasAnyChildren)
        {
            foreach (var child in startCell.Children)
            {
                cursor.Y += 1;
                ExtendToRectange(child, cursor);
                cursor.Y -= 1;
                if (cursor.RowSpanDebt != 0)
                {
                    startCell.Placement = startCell.Placement.ChangeSize(rows: cursor.RowSpanDebt + 1);
                    cursor.RowSpanDebt = 0;
                }
            }
        }
        else
        {
            int rowDiff = cursor.MaxY - cursor.Y;
            if (!startCell.IsFixed)
            {
                startCell.Placement = startCell.Placement.ChangeSize(rows: rowDiff);
                cursor.RowSpanDebt = 0;
            }
            else
            {
                cursor.RowSpanDebt = rowDiff;
            }
        }
    }
    // вызывается после всех методов и добавляет нумерацию к столбцам
    private void AddNumeration(ColumnHeaderCell<T> parent, ref int startNumber)
    {
        if (parent.Children.Any())
        {
            foreach (var child in parent.Children)
            {
                AddNumeration(child, ref startNumber);
            }
        }
        else
        {
            var numericColumn = new ColumnHeaderCell<T>(startNumber.ToString(), parent,
            new CellPlacement(x: parent.Placement.X, parent.Placement.Y + 1, 1, 1));
            startNumber += 1;
        }
    }
    // выполняет делегат по дереву (дерево предполагается полностью инициализированным)
    private void TraceTree(Action<ColumnHeaderCell<T>> toPerform, ColumnHeaderCell<T> start)
    {
        toPerform.Invoke(start);
        if (start.HasAnyChildren)
        {
            foreach (var cell in start.Children)
            {
                TraceTree(toPerform, cell);
            }
        }
    }
    // координата x абсолютная и начинается с самой крайней левой ячейки
    public ColumnHeaderCell<T> TraceVertical(int x)
    {
        ColumnHeaderCell<T>? found = null;
        Action<ColumnHeaderCell<T>> cellGetter =
        (cell) =>
        {
            if (found is not null)
            {
                return;
            }
            if (cell.Placement.X == x && !cell.HasAnyChildren)
            {
                found = cell;
            }
        };
        TraceTree(cellGetter, _root);
        if (found is null)
        {
            throw new Exception("Заголовок стобца не может быть не найден");
        }
        return found;

    }
    // метод преобразует граф в разметку HTML
    // предполагается, что после конструирования и нормализации граф не менялся
    public string ToHTMLTableHead()
    {
        var builder = new TableHeaderBuilder<T>();
        int logicalHeight = 1;

        bool found = false;
        Action<ColumnHeaderCell<T>> cellGetter = (cell) =>
        {
            // пропуск корня, он не участвует в разметке  
            if (object.ReferenceEquals(_root, cell))
            {
                return;
            }
            // добавление всех подходящих заголовков в линию
            if (logicalHeight - cell.GetTreeGeometricalHeight() == 0 && cell.GetTreeLogicalHeight() != 1 || (cell.GetTreeLogicalHeight() == 1 && logicalHeight == 1))
            {
                builder.AddHeader(cell);
                found = true;
            }
        };

        do
        {
            found = false;
            TraceTree(cellGetter, _root);
            builder.FinishRow();
            // за раз конструируется один уровень заголовка
            logicalHeight++;
        }
        while (found);
        if (logicalHeight == 0)
        {
            throw new Exception("Граф шапки таблицы имеет недостаточную глубину");
        }
        return builder.ToString();
    }
}

public class TableHeaderBuilder<T>
{

    private StringBuilder _html;
    private StringBuilder _currentRow;
    public TableHeaderBuilder()
    {
        _html = new StringBuilder();
        _currentRow = new StringBuilder();
    }
    public void AddHeader(ColumnHeaderCell<T> cell)
    {
        _currentRow.Append($"<th class = \"thStats\" rowspan=\"{cell.Placement.RowSpan}\" colspan=\"{cell.Placement.ColumnSpan}\">{cell.Name}</th>");
    }
    public void FinishRow()
    {
        if (_currentRow.Length == 0)
        {
            return;
        }
        _html.Append("<tr>" + _currentRow.ToString() + "</tr>" + "\n");
        _currentRow.Clear();
    }
    public override string ToString()
    {
        return "<thead>" + _html.ToString() + "</thead>";
    }

}

public class HeaderBuilderCursor
{

    private int _y;
    private int _x;
    private int _maxY;
    private int _maxX;
    public int X
    {
        get => _x;
        set
        {
            if (value > _maxX)
            {
                _maxX = value;
            }
            _x = value;
        }
    }
    public int Y
    {
        get => _y;
        set
        {
            if (value > _maxY)
            {
                _maxY = value;
            }
            _y = value;
        }
    }

    public int RowSpanDebt { get; set; }
    public int MaxY { get => _maxY; }
    public int MaxX { get => _maxX; }

    public HeaderBuilderCursor()
    {
        _y = 0;
        _x = 0;
    }




}




