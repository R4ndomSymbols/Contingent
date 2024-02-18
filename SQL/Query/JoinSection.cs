namespace StudentTracking.SQL;

public class JoinSection : IQueryPart{

    private List<string> _joins;

    public JoinSection(){
        _joins = new List<string>();
    }

    public JoinSection AppendJoin(JoinType type, Column sourceTableColumn, Column joinColumn){
        string prefix = _joinPrefixes[type];
        _joins.Add(
            prefix + " " + joinColumn.TableName + " ON " +  sourceTableColumn.AsSQLText() + " = " + joinColumn.AsSQLText()
        );
        return this;
    }
    // удостоверится, что методы сравнения ключей переопределены
    public static JoinSection? FindJoinRoute(string sourceTableName, IEnumerable<Column> mustBeSelected){
        // не работает для составных ключей
        
        var toExclude = mustBeSelected.ToList();
        var root = DatabaseGraph.Instance.GetByName(sourceTableName);
        if (root is null){
            throw new Exception("Необходимая для выборки таблица не зарегистрирована");
        }
        // проверка по имеющимся внешним ключам у таблицы
        // последовательность не гарантированна
        // но, определенно, тут будут все нужные join
        var joinSequence = new List<(Column sourceCol, Column joinCol)>();
        
        // работает только для низходящего join
        // внешний ключ родительской таблицы -> приватный ключ дочерней        
        Action<SQLTable, SQLTable> joinUpReminder = (tableFrom, tableTo) => {
            // если исключать больше нечего можно прекращать поиск
            if (!toExclude.Any()){
                return;
            }
            IEnumerable<Column>? found = new List<Column>(); 
            // если tableTo is null, то это самопроверка таблицы на предмет наличия нужных столбцов
            if (tableTo is null){
                ProcessSelf(tableFrom);
            }
            else{
                // поиск совпадений в новой таблице
                found = toExclude.Where(
                (col) => tableTo.TableName == col.TableName 
                && tableTo.DataColumns.Any(tcol => tcol.ColumnName == col.Name) 
                );
                if (found.Any() && !tableTo.Equals(root)){
                    // предполагается, что таблицы могут быть соединены только по ключу
                    // если соединение осуществляется не по нему, то определить, как именно проводить join 
                    // автоматически невозможно
                    // получение ключа исходной таблицы
                    var sourceTableCol = tableFrom.ForeignKeys.Where(x => x.Reference == tableTo.Primary).First();
                    joinSequence.Add((
                        // join на по внешнему ключу, который совпадает с приватным ключем другой таблицы
                        new Column(sourceTableCol.ColumnName, tableFrom.TableName),
                        new Column(tableTo.Primary.ColumnName, tableTo.TableName)));
                    RemoveFromExcludeLog(found);
                }
            }
        };
        // другая вариация поиска
        // приватный ключ родительской таблицы - множество внешних ключей для другой таблицы
        Action<SQLTable, IEnumerable<SQLTable>> joinDownReminder = (tableFrom, tablesTo) => {
            if (!toExclude.Any()){
                return;
            }
            if (tablesTo is null || !tablesTo.Any()){
                ProcessSelf(tableFrom);
                return;
            }
            IEnumerable<Column> found = new List<Column>();
            // если связанные таблицы есть, то обследовать их
            if (tablesTo.Any()){
                foreach (var table in tablesTo){
                    found = toExclude.Where(
                        (col) => col.TableName == table.TableName && table.DataColumns.Any(datacol => datacol.ColumnName == col.Name) 
                    );
                    if (found.Any() && !table.Equals(tableFrom)){
                        var associatedForeign = table.ForeignKeys.Where(x => x.Reference == tableFrom.Primary).First();
                        joinSequence.Add((
                            new Column(tableFrom.Primary.ColumnName, tableFrom.TableName),
                            new Column(associatedForeign.ColumnName, tableFrom.TableName)
                        ));
                        RemoveFromExcludeLog(found);
                    }
                }
            }
        };

        // от внешних к приватным
        TraceGraphUp(joinUpReminder, root, new Stack<SQLTable>());
        // от приватных к внешним
        TraceGraphDown(joinDownReminder, root, new Stack<SQLTable>());

        if (toExclude.Any()){
            throw new Exception("Не все колонки оказались найденными, такого не должно быть");
        }
        if (!joinSequence.Any()){
            return null;
        }
        var toReturn = new JoinSection();
        foreach (var rec in joinSequence){
            toReturn.AppendJoin(JoinType.InnerJoin, rec.sourceCol, rec.joinCol);
        }
        return toReturn;


        void ProcessSelf(SQLTable thisTable){
            if (!toExclude.Any()){
                return;
            }
            var found = toExclude.Where(
            (col) => thisTable.TableName == col.TableName 
            && thisTable.DataColumns.Any(tcol => tcol.ColumnName == col.Name) 
            );
            RemoveFromExcludeLog(found);
        }

        void RemoveFromExcludeLog(IEnumerable<Column> columns){
            if (columns.Any()){
                toExclude.RemoveAll(x => columns.Any(y => y == x));
            }
            
        }



        // множество раз проверяет таблицу родитель
        void TraceGraphUp(Action<SQLTable, SQLTable> toPerform, SQLTable start, Stack<SQLTable> path){
            path.Push(start);
            toPerform.Invoke(start, null);
            foreach(var key in start.ForeignKeys){
                if (path.Any(x => x == key.Reference.InTable)){
                    continue;
                }
                toPerform.Invoke(start, key.Reference.InTable);
                TraceGraphUp(toPerform, key.Reference.InTable, path);
            }
        }
        void TraceGraphDown(Action<SQLTable, IEnumerable<SQLTable>> toPerform, SQLTable start, Stack<SQLTable> path){
            path.Push(start);
            // вызов для себя
            toPerform.Invoke(start, new List<SQLTable>());
            var references = DatabaseGraph.Instance.GetAllReferencesToPrimaryKey(start);
            // вызов для ссылающихся таблиц
            toPerform.Invoke(start, references);
            foreach (var table in references){
                if (path.Any(x => x == table)){
                    continue;
                }
                TraceGraphDown(toPerform, table, path);
            }
        }
    }

    public string AsSQLText()
    {
        return string.Join("\n", _joins);
    }

    public enum JoinType {
        LeftJoin = 1,
        RightJoin = 2,
        FullJoin = 3,
        InnerJoin = 4
    }

    private static readonly Dictionary<JoinType, string> _joinPrefixes = new Dictionary<JoinType, string>{
        {JoinType.FullJoin, "FULL OUTER JOIN"},
        {JoinType.RightJoin, "RIGHT JOIN"},
        {JoinType.LeftJoin, "LEFT JOIN"},
        {JoinType.InnerJoin, "INNER JOIN"}
    };    

}



