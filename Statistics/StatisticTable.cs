using System.Drawing;
using System.Text;

namespace StudentTracking.Statistics;

public class StatisticTable<M> {
    
    private TableColumnHeader<M> _columnHeaders;
    private TableRowHeader<M> _rowHeaders;
    // начало (левый верхний угол таблицы)
    private Point _startPoint;
    // конец (правый нижний угол таблицы)
    private Point _endPoint;
    private StatisticTableCell[][] _content;

    private IEnumerable<M> _tableDataSource;

    public string TableName {get; private init;}

    public StatisticTable(TableColumnHeader<M> tableColumnHeader, TableRowHeader<M> rowHeader, IEnumerable<M> dataSource, string tableName){
        _columnHeaders = tableColumnHeader;
        _rowHeaders = rowHeader;
        _startPoint = new Point(
            _rowHeaders.HeaderWidth,
            _columnHeaders.HeaderHeigth
        );
        _endPoint = new Point(
            _columnHeaders.HeaderWidth - 1,
            _rowHeaders.HeaderHeigth - 1
        );
        _tableDataSource = dataSource;
        TableName = tableName;
        Populate();
    }

    private void Populate(){
        // [y][x]
        var tableWidth = _endPoint.X - _startPoint.X + 1;
        var tableHeigth = _endPoint.Y - _startPoint.Y + 1;
        Console.WriteLine(_startPoint);
        Console.WriteLine(_endPoint);
        _content = new StatisticTableCell[tableHeigth][];
        for(int y = 0; y < tableHeigth; y++){
            var realY = y + _startPoint.Y; 
            _content[y] = new StatisticTableCell[tableWidth];
            for (int x = 0; x < tableWidth; x++)
            { 
                var realX = x+_startPoint.X;
                var cell = new StatisticTableCell(realX, realY);
                var query = GetFilterForCell(cell);
                cell.StatsGetter = () => {
                    return new CountResult(query.Execute(_tableDataSource).Count());
                }; 
                _content[y][x] = cell;
            }
        }
    }

    private Filter<M> GetFilterForCell(StatisticTableCell cell){
        var constraintColumnHeader = _columnHeaders.TraceVertical(cell.X);
        var constraintRowHeader = _rowHeaders.TraceHorizontal(cell.Y);
        return constraintColumnHeader.NodeFilter.Include(constraintRowHeader.NodeFilter);
    }

    public string ToHtmlTable(){
        var thead = _columnHeaders.ToHTMLTableHead();
        var tbody = _rowHeaders.SetHeaders();
        foreach (var row in tbody){
            // [y][x]
            var cellRow = _content[row.Y - _rowHeaders.HeaderOffset];
            foreach (var cell in cellRow){
                row.AppendCell(cell.StatsGetter.Invoke().ToString());
            } 
        }
        var bodyHTML = new StringBuilder();
        foreach (var row in tbody){
            bodyHTML.Append(row.ToString());
        }
        return 
        "<table class = \"tableStats\">" 
        + thead
        + "<tbody>" + bodyHTML + "</tbody>"
        + "</table>";  
    
    }
}
