using System.Runtime.CompilerServices;

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
    public int HeaderWidth => _numericRows is null ? 1 : 2;
    public int HeaderHeigth => _rowsHeadersStrictOrder.Count + HeaderOffset - 1;
    // смещение в координатной сетке всей таблицы
    // заголовок стобца всегда стоит выше заголовка строки 
    public int HeaderOffset {get; private set;} 
    private bool _useNumeration;
    public TableRowHeader(ConstrainedRowHeaderCell root, TableColumnHeader tableHeader, bool useNumeration){
        _useNumeration = useNumeration;
        HeaderOffset = tableHeader.HeaderHeigth;
        _root = root;
    }
    // добавить методы быстрого добавления агрегатов и т.д.
    public void AddRow(ConstrainedRowHeaderCell rowHeader){
        _rowsHeadersStrictOrder.Add(rowHeader);
        if (!rowHeader.IsOnlyHeader){
            var nextNumber = _numericRows.Count + 1;
            _numericRows.Add(new ConstrainedRowHeaderCell(nextNumber.ToString()));
        }
    }
    private void Normalize(){
        var cursor = new HeaderBuilderCursor();
        // начало отсчета - левый нижний угол шапки
        cursor.Y = HeaderOffset;
        cursor.X = 0;
    

        void Normalize(ConstrainedRowHeaderCell start, HeaderBuilderCursor cursor){
            if (start.HasAnyChildren){
                var cPlace = start.Placement; 
                cPlace.X = cursor.X;
                cPlace.Y = cursor.Y;
                foreach (var child in start.Children){
                    cursor.X+=1;
                    Normalize(child, cursor);
                    cursor.X-=1;
                    cPlace.ChangeSize(child.Placement.RowSpan, 1);
                }
            }
            else {
                start.Placement = new CellPlacement(cursor.X, cursor.Y, 1, 1);
            }
        } 

    } 



    public IEnumerable<ConstrainedRowHeaderCell> TraceHorizontal(int y){
        return _rowsHeadersStrictOrder.Where(x => x.Y == y);
    }
}


