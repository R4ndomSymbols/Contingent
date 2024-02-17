namespace StudentTracking.Statistics;

public class TableRowHeader {

    private List<ConstrainedRowHeaderCell> _rowsHeadersStrictOrder;
    private List<ConstrainedRowHeaderCell> _numericRows;

    public int HeaderWidth => _numericRows is null ? 1 : 2;
    public int HeaderHeigth => _rowsHeadersStrictOrder.Count; 

    private bool _useNumeration;
    public TableRowHeader(bool useNumeration){
        _useNumeration = useNumeration;
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
}


