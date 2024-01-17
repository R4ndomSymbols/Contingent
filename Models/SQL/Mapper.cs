using Npgsql;

namespace StudentTracking.Models.SQL;

public class Mapper<T> {

    private Action<T, NpgsqlDataReader> _map;
    private List<Column> _columns;
    public Mapper (Action<T, NpgsqlDataReader> map, List<Column> mappedColumns){
        _map = map;
        _columns = mappedColumns;
    }
    public void Map(T objectToMap, NpgsqlDataReader reader){
        _map.Invoke(objectToMap, reader);
    }

    public IReadOnlyList<Column> GetSelectedColumns(){
        return _columns.AsReadOnly();
    }
}
