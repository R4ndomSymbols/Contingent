using System.Drawing;
using System.Text;
using Microsoft.AspNetCore.Mvc.TagHelpers;
using StudentTracking.SQL;
using Utilities;

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

    public StatisticTable(TableColumnHeader<M> tableColumnHeader, TableRowHeader<M> rowHeader, IEnumerable<M> dataSource){
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
        Populate();
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
                var cell = new StatisticTableCell(j, i);
                var query = GetFilterForCell(cell);
                cell.StatsGetter = () => {
                    var result = query.Execute(_tableDataSource);
                    return Task.Run(() => new CountResult(result.Count()));
                }; 
                _content[i - _offsetY][j] = cell;
            }
        }
    }

    private Filter<M> GetFilterForCell(StatisticTableCell cell){
        var constraintColumnHeader = _columnHeaders.TraceVertical(cell.X);
        var constraintRowHeader = _rowHeaders.TraceHorizontal(cell.Y);
        return constraintRowHeader.GetFilterSequence().Merge(constraintRowHeader.GetFilterSequence());
    }

    public async Task<string> ToHtmlTable(){
        var thead = _columnHeaders.ToHTMLTableHead();
        var tbody = _rowHeaders.SetHeaders();
        foreach (var row in tbody){
            // [y][x]
            var cellRow = _content[row.Y - _rowHeaders.HeaderOffset];
            foreach (var cell in cellRow){
                row.AppendCell((await cell.StatsGetter.Invoke()).ToString());
            } 
        }
        var bodyHTML = new StringBuilder();
        foreach (var row in tbody){
            bodyHTML.Append(row.ToString());
        }
        return 
        "<table class = \"table\">" 
        + thead
        + "<tbody>" + bodyHTML + "</tbody>"
        + "</table>";  
    
    }
}
