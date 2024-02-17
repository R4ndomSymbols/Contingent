using System.Drawing;

namespace StudentTracking.Statistics;

public class StatisticTable {
    
    private TableColumnHeader _columnHeaders;
    private TableRowHeader _rowHeaders;
    // начало (левый верхний угол таблицы)
    private Point _startPoint;
    // конец (правый нижний угол таблицы)
    private Point _endPoint;

    private StatisticTableCell[][] _content;
 
    public StatisticTable(TableColumnHeader tableColumnHeader, TableRowHeader rowHeader){
        _columnHeaders = tableColumnHeader;
        _rowHeaders = rowHeader;
        _startPoint = new Point(
            _rowHeaders.HeaderWidth,
            _columnHeaders.HeaderHeigth
        );
        _endPoint = new Point(
            _columnHeaders.HeaderLength - 1,
            _rowHeaders.HeaderHeigth - 1
        );
    }

    private void Populate(){
        // [y][x]
        var tableWidth = _endPoint.X - _startPoint.X;
        var tableHeigth = _endPoint.Y - _startPoint.Y;
        _content = new StatisticTableCell[tableHeigth][];
        var _offsetY = _startPoint.Y;
        var _offsetX = _startPoint.X;
        for(int i = _startPoint.Y; i < _endPoint.Y; i++){
            _content[i - _offsetY] = new StatisticTableCell[tableWidth];
            for (int j = _startPoint.X; j < _endPoint.X; j++){
                _content[j - _offsetX] = 
                new StatisticTableCell
            }
        }

    }






}
