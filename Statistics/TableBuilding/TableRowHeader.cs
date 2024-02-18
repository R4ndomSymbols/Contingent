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

    private List<ConstrainedRowHeaderCell> _rowsHeadersStrictOrder;
    private List<ConstrainedRowHeaderCell> _numericRows;

    public int HeaderWidth => _numericRows is null ? 1 : 2;
    public int HeaderHeigth => _rowsHeadersStrictOrder.Count + HeaderOffset - 1;
    public int HeaderOffset {get; private set;} 

    private bool _useNumeration;
    public TableRowHeader(bool useNumeration, TableColumnHeader tableHeader){
        _useNumeration = useNumeration;
        HeaderOffset = tableHeader.HeaderHeigth;
        if (_useNumeration){
            _numericRows = new List<ConstrainedRowHeaderCell>();
        }
        _rowsHeadersStrictOrder = new List<ConstrainedRowHeaderCell>();
    }
    // добавить методы быстрого добавления агрегатов и т.д.
    public void AddRow(ConstrainedRowHeaderCell rowHeader){
        _rowsHeadersStrictOrder.Add(rowHeader);
        if (!rowHeader.IsOnlyHeader){
            var nextNumber = _numericRows.Count + 1;
            _numericRows.Add(new ConstrainedRowHeaderCell(nextNumber.ToString()));
        }
    }

    public IEnumerable<ConstrainedRowHeaderCell> TraceHorizontal(int y){
        return _rowsHeadersStrictOrder.Where(x => x.Y == y);
    }
}


