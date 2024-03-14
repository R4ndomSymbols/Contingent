using Npgsql;
using Utilities;

namespace StudentTracking.SQL;

public class Mapper<T> : Mapper, IQueryPart {

    private Func<NpgsqlDataReader, QueryResult<T>> _map;
    private List<Column> _columns;
    public override JoinSection PathTo {get; protected set;}
    public override IReadOnlyCollection<Column> Columns 
    { 
        get => _columns; 
    }
    public Mapper (Func<NpgsqlDataReader, QueryResult<T>> map, IReadOnlyCollection<Column> mappedColumns){
        _map = map;
        _columns = mappedColumns.ToList();
        PathTo = new JoinSection();
    }

    public QueryResult<T> Map(NpgsqlDataReader reader){
        return _map.Invoke(reader);
    }

    public string AsSQLText(Column distinctOn)
    {
        return "SELECT "+ "DISTINCT ON (" + distinctOn.AsSQLText() + ") " + string.Join(", ", _columns.Select(x => x.AsSQLText())); 
    }
    public string AsSQLText()
    {
        return "SELECT " + string.Join(", ", _columns.Select(x => x.AsSQLText())); 
    }

    public override void AssumeChild(Mapper mapper)
    {
        PathTo.AppendJoin(mapper.PathTo);
        _columns.AddRange(mapper.Columns);
    }
}

public abstract class Mapper {

    public virtual JoinSection PathTo {get; protected set;}
    public virtual IReadOnlyCollection<Column> Columns {get;}
    public abstract void AssumeChild(Mapper mapper);

}

