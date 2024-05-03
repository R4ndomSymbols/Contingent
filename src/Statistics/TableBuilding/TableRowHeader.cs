using System.Text;

namespace Contingent.Statistics;

/*
    |0 1 | 2 3   4 5 |
    |1 x | x x | x x |
    ------------------ 
     2 x   x
     3 x
     4 x
*/

public class TableRowHeader<T>
{
    // так же как для колонок, не отрисовывается
    private RowHeaderCell<T> _root;
    public int HeaderWidth { get; private set; }
    public int HeaderHeigth { get; private set; }
    // смещение в координатной сетке всей таблицы
    // заголовок стобца всегда стоит выше заголовка строки 
    public int HeaderOffset { get; private set; }
    private bool _useNumeration;
    public TableRowHeader(RowHeaderCell<T> root, TableColumnHeader<T> tableHeader, bool useNumeration)
    {
        _useNumeration = useNumeration;
        HeaderOffset = tableHeader.HeaderHeigth;
        // Point[headerOffset, - 1] - начальная клетка таблицы
        _root = root;
        Normalize(out HeaderBuilderCursor cursor);

        // дебаг
        Action<RowHeaderCell<T>> printCords = (cell) => Console.WriteLine(cell.Placement + " " + cell.Name);
        TraceTree(printCords, _root);

    }
    // не оптимальное решение
    private void CheckCycle(RowHeaderCell<T> current, List<RowHeaderCell<T>> log)
    {
        if (log.Any(node => object.ReferenceEquals(node, current)))
        {
            throw new Exception("Обнаружен цикл в графе заголовков строк");
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
        // начало отсчета - левый нижний угол шапки
        cursor.Y = HeaderOffset;
        cursor.X = -1;
        if (_useNumeration)
        {
            AddNumeration(_root);
        }
        // создание сетки
        Normalize(_root, cursor);
        ExtendToRectangle(_root, cursor);
        // Из-за алгоритма работы normalize 
        // происходит одно лишнее смещение
        HeaderHeigth = cursor.MaxY;
        HeaderWidth = cursor.MaxX + 1;
    }

    void ExtendToRectangle(RowHeaderCell<T> start, HeaderBuilderCursor egdeStore)
    {
        Action<RowHeaderCell<T>> cellExpander = (cell) =>
        {
            if (!cell.HasAnyChildren)
            {
                var place = cell.Placement;
                // корректировка colspan для единой максимальной ширины колонки
                cell.Placement = place.ChangeSize(0, egdeStore.MaxX - place.X);
            }
        };
        TraceTree(cellExpander, start);
    }
    void Normalize(RowHeaderCell<T> start, HeaderBuilderCursor cursor)
    {
        if (start.HasAnyChildren)
        {
            var cPlace = start.Placement;
            cPlace.X = cursor.X;
            cPlace.Y = cursor.Y;
            cPlace.ColumnSpan = 1;
            foreach (var child in start.Children)
            {
                cursor.X += 1;
                Normalize(child, cursor);
                cursor.X -= 1;
                cPlace = cPlace.ChangeSize(child.Placement.RowSpan, 0);
            }
            // обновление размера после проверки всех потомков
            start.Placement = cPlace;
        }
        else
        {
            start.Placement = new CellPlacement(cursor.X, cursor.Y, 1, 1);
            cursor.Y++;
        }
    }

    private void AddNumeration(RowHeaderCell<T> root)
    {

        int numberStore = 1;
        Action<RowHeaderCell<T>> numerationAdder = (cell) =>
        {
            if (!cell.HasAnyChildren)
            {
                var numberNode = new RowHeaderCell<T>(numberStore.ToString(), cell);
                // номер - дочерняя нода с пустым условием
                numberStore++;
            }
        };
        TraceTree(numerationAdder, root);
        // поправка на новую клетку
    }

    private void TraceTree(Action<RowHeaderCell<T>> toPerform, RowHeaderCell<T> start)
    {
        if (start.HasAnyChildren)
        {
            foreach (var child in start.Children)
            {
                TraceTree(toPerform, child);
            }
        }
        toPerform.Invoke(start);
    }

    public TableRow[] SetHeaders()
    {
        var result = new TableRow[HeaderHeigth - HeaderOffset];

        // берутся только те клетки, у которых совпадает Y (а не находится в рамках клетки)

        var found = new List<RowHeaderCell<T>>();
        for (int i = 0; i < result.Length; i++)
        {
            found.Clear();
            var realY = HeaderOffset + i;
            var newNode = new TableRow(realY);
            FindCells(_root, realY, found);
            found.ForEach(c => newNode.AppendHeader(c));
            result[i] = newNode;
        }

        return result;

        void FindCells(RowHeaderCell<T> current, int yToFind, List<RowHeaderCell<T>> acc)
        {
            if (current.Placement.Y == yToFind && !object.ReferenceEquals(current, _root))
            {
                acc.Add(current);
            }
            if (current.HasAnyChildren)
            {
                foreach (var child in current.Children)
                {
                    FindCells(child, yToFind, acc);
                }
            }
        }
    }

    // реализация условий с помощью фильтров
    public RowHeaderCell<T> TraceHorizontal(int yInTable)
    {
        RowHeaderCell<T>? outerCell = null;
        Action<RowHeaderCell<T>> cellGetter = (cell) =>
        {
            if (outerCell is not null)
            {
                return;
            }
            // клетка должна быть внешней и Y координата должна совпадать
            if (!cell.HasAnyChildren && cell.Placement.Y == yInTable)
            {
                outerCell = cell;
            }
        };
        TraceTree(cellGetter, _root);
        if (outerCell is null)
        {
            Console.WriteLine("error y: " + yInTable);
            throw new Exception("Невозможна ситуация неполучения клетки");
        }
        return outerCell;
    }
}


public class TableRow
{
    private StringBuilder _content;
    private StringBuilder _headers;
    public int Y { get; private init; }
    public TableRow(int y)
    {
        Y = y;
        _content = new StringBuilder();
        _headers = new StringBuilder();
    }
    public void AppendHeader<T>(RowHeaderCell<T> header)
    {
        _headers.Append($"<th class = \"thStats\" colspan = \"{header.Placement.ColumnSpan}\" rowspan= \"{header.Placement.RowSpan}\">{header.Name}</th>");
    }

    public void AppendCell(string cellContent)
    {
        _content.Append("<td class = \"tdStats\">" + cellContent + "</td>");
    }

    public override string ToString()
    {
        return "<tr >" + _headers.ToString() + _content.ToString() + "</tr>";
    }
}

