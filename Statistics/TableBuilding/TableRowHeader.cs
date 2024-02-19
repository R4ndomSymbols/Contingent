using System.Runtime.CompilerServices;
using System.Text;

namespace StudentTracking.Statistics;

/*
    |0 1 | 2 3   4 5 |
    |1 x | x x | x x |
    ------------------ 
     2 x   x
     3 x
     4 x
*/

public class TableRowHeader {
    // так же как для колонок, не отрисовывается
    private ConstrainedRowHeaderCell _root;
    public int HeaderWidth {get; private set;}
    public int HeaderHeigth {get; private set;}
    // смещение в координатной сетке всей таблицы
    // заголовок стобца всегда стоит выше заголовка строки 
    public int HeaderOffset {get; private set;} 
    private bool _useNumeration;
    public TableRowHeader(ConstrainedRowHeaderCell root, TableColumnHeader tableHeader, bool useNumeration){
        _useNumeration = useNumeration;
        HeaderOffset = tableHeader.HeaderHeigth;
        _root = root;
        Normalize();
        if (_useNumeration){
            AddNumeration(_root);
        }

    }

    private void Normalize(){
        var cursor = new HeaderBuilderCursor();
        // начало отсчета - левый нижний угол шапки
        cursor.Y = HeaderOffset;
        cursor.X = 0;
        // создание сетки
        Normalize(_root, cursor);
        // расширение сетки до прямоугольника
        ToRectangle(_root, cursor);

        HeaderHeigth = cursor.MaxY;
        HeaderWidth= cursor.MaxX;

        void Normalize(ConstrainedRowHeaderCell start, HeaderBuilderCursor cursor){
            if (start.HasAnyChildren){
                var cPlace = start.Placement; 
                cPlace.X = cursor.X;
                cPlace.Y = cursor.Y;
                cPlace.ColumnSpan = 1;
                foreach (var child in start.Children){
                    cursor.X+=1;
                    Normalize(child, cursor);
                    cursor.X-=1;
                    cPlace.ChangeSize(child.Placement.RowSpan, 0);
                }
                // обновление размера после проверки всех потомков
                start.Placement = cPlace;
            }
            else {
                start.Placement = new CellPlacement(cursor.X, cursor.Y, 1, 1);
            }
        }

        void ToRectangle(ConstrainedRowHeaderCell start, HeaderBuilderCursor egdeStore){
            Action<ConstrainedRowHeaderCell> cellExpander = (cell) => {
                if (!cell.HasAnyChildren){
                    var place = cell.Placement;  
                    // корректировка colspan для единой максимальной ширины колонки
                    cell.Placement = place.ChangeSize(0, egdeStore.MaxX - place.X);
                }
            };
            TraceTree(cellExpander, start);
        }
    }
    // работает только с прямоугольным представлением заголовка
    private void AddNumeration(ConstrainedRowHeaderCell root){

        int numberStore = 1;
        Action<ConstrainedRowHeaderCell> numerationAdder = (cell) => {
                if (!cell.HasAnyChildren){
                    // пустые заголовки не отмечаются цифрами и не влияют на счет
                    string cellText = cell.IsOnlyHeader ? " " : numberStore.ToString();
                    var numberNode = new ConstrainedRowHeaderCell(cellText, null, cell, null);
                    numberNode.Placement = new CellPlacement(cell.Placement.X+1, cell.Placement.Y, 1, 1);
                    // номер - дочерняя нода с пустым условием
                    cell.AddChild(numberNode);
                    if (!cell.IsOnlyHeader){
                        numberStore++;
                    }
                    
                }
            };
        TraceTree(numerationAdder, root);
        // поправка на новую клетку
        HeaderWidth +=1;
    } 

    private void TraceTree(Action<ConstrainedRowHeaderCell> toPerform, ConstrainedRowHeaderCell start){
        toPerform.Invoke(start);
        if (start.HasAnyChildren){
            foreach (var child in start.Children){
                TraceTree(toPerform, child);
            }
        }
    }

    public TableRow[] SetHeaders(){
        var result = new TableRow[HeaderHeigth];

        // берутся только те клетки, у которых совпадает Y (а не находится в рамках клетки)
        
        var found = new List<ConstrainedRowHeaderCell>();
        for (int i = 0; i < HeaderHeigth; i++){
            found.Clear();
            var realY = HeaderOffset + i;
            var newNode = new TableRow(realY);
            FindCells(_root, realY, found);
            found.ForEach(c => newNode.AppendHeader(c));
            result[i] = newNode;
        }

        return result;

        void FindCells(ConstrainedRowHeaderCell current, int yToFind, List<ConstrainedRowHeaderCell> acc){
            if (current.Placement.Y == yToFind  &&  object.ReferenceEquals(current, _root)){
                acc.Add(current);
            }
            if (current.HasAnyChildren){
                foreach(var child in current.Children){
                    FindCells(child, yToFind, acc);
                }
            }
        }
    }




    // попробовать реализовать получение условий из клетки
    // таким образом, можно инкапсулировать слияние условий
    public ConstrainedRowHeaderCell TraceHorizontal(int yInTable){
        ConstrainedRowHeaderCell? outerCell = null;
        Action<ConstrainedRowHeaderCell> cellGetter = (cell) => {
            if (outerCell is not null){
                return;
            }
            // клетка должна быть внешней и Y координата должна совпадать
            if (!cell.HasAnyChildren && cell.Placement.Y == yInTable){
                outerCell = cell;
            }
        };
        if (outerCell is null){
            throw new Exception("Невозможна ситуация неполучения клетки");
        }
        return outerCell;
    }
}


public class TableRow {
    private StringBuilder _content;
    private StringBuilder _headers;
    public int Y {get; private init;} 
    public TableRow(int y) {
        Y = y;
        _content = new StringBuilder();
        _headers = new StringBuilder();
    }
    public void AppendHeader(ConstrainedRowHeaderCell header){
        _headers.Append($"<th colspan = \"{header.Placement.ColumnSpan}\" rowspan= \"{header.Placement.RowSpan}\">{header.Name}</th>"); 
    }

    public void AppendCell(string cellContent){
        _content.Append("<td>" + cellContent + "</td>");
    }

    public override string ToString()
    {
        return "<tr>" + _headers.ToString() + _content.ToString() + "</tr>";
    }
}

