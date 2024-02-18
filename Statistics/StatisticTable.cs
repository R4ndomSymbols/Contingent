using System.Drawing;
using StudentTracking.SQL;
using Utilities;

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
                var query = BuildQueryForCell(cell);
                cell.StatsGetter = () => {
                    using var conn = Utils.GetAndOpenConnectionFactory().Result;
                    var result = query.Execute(conn, new QueryLimits(0, 1000)).Result; 
                    return Task.Run(() => result.First());
                }; 
                _content[i - _offsetY][j] = cell;
            }
        }
    }

    private SelectQuery<CountResult> BuildQueryForCell(StatisticTableCell cell){
        var constraintColumnHeaders = _columnHeaders.TraceVertical(cell.X);
        var constraintRowHeaders = _rowHeaders.TraceHorizontal(cell.Y);
        var totalWhere = constraintColumnHeaders.Select(x => x.Constraint).Concat(constraintColumnHeaders.Select(x => x.Constraint));
        // слияние всех условий для клеточки таблицы
        var clause = ComplexWhereCondition.Unite(ComplexWhereCondition.ConditionRelation.AND, totalWhere);
        // select подразумевает, что первичной таблицей является таблица студентов
        var mapper = new Mapper<CountResult>(
            (m) => {
                var count = (int)m["found_count"];
                return Task.Run(() => QueryResult<CountResult>.Found(new CountResult(count)));
            },
            new List<Column>(){
                new Column("COUNT", "id", "students","found_count")
            });

        JoinSection? joins = null;
        if (clause is not null){
            joins = JoinSection.FindJoinRoute("students", clause.GetAllRestrictedColumns()); 
        }
        var select = SelectQuery<CountResult>.Init("students").
        AddMapper(mapper).
        AddJoins(joins).
        AddWhereStatement(clause).Finish();
        if (select.IsFailure){
            throw new Exception("При генерации запроса для клетки таблицы он не может провалиться");
        }
        return select.ResultObject;
    }

    





}
