using Npgsql;

namespace StudentTracking.Models.SQL;

public class Mapper<T> : IQueryPart {

    private Func<NpgsqlDataReader, Task<T>> _map;
    private IReadOnlyCollection<Column> _columns;
    public Mapper (Func<NpgsqlDataReader, Task<T>> map, IReadOnlyCollection<Column> mappedColumns){
        _map = map;
        _columns = mappedColumns;
    }
    public async Task<T> Map(NpgsqlDataReader reader){
        return await _map.Invoke(reader);
    }

    public IReadOnlyCollection<Column> GetSelectedColumns(){
        return _columns;
    }

    public string AsSQLText()
    {
        return "SELECT " + string.Join(", ", _columns.Select(x => x.AsSQLText())); 
    }
}
