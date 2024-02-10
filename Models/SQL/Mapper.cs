using Npgsql;
using Utilities;

namespace StudentTracking.Models.SQL;

public class Mapper<T> : IQueryPart {

    private Func<NpgsqlDataReader, Task<QueryResult<T>>> _map;
    private IReadOnlyCollection<Column> _columns;
    public Mapper (Func<NpgsqlDataReader, Task<QueryResult<T>>> map, IReadOnlyCollection<Column> mappedColumns){
        _map = map;
        _columns = mappedColumns;
    }
    public async Task<QueryResult<T>> Map(NpgsqlDataReader reader){
        return await _map.Invoke(reader);
    }

    public IReadOnlyCollection<Column> GetSelectedColumns(){
        return _columns;
    }

    public string AsSQLText(Column distinctOn)
    {
        return "SELECT "+ "DISTINCT ON (" + distinctOn.AsSQLText() + ") " + string.Join(", ", _columns.Select(x => x.AsSQLText())); 
    }
    public string AsSQLText()
    {
        return "SELECT " + string.Join(", ", _columns.Select(x => x.AsSQLText())); 
    }
}
