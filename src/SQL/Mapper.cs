using Npgsql;
using Contingent.Utilities;

namespace Contingent.SQL;

public class Mapper<T> : Mapper, IQueryPart
{

    private Func<NpgsqlDataReader, QueryResult<T>>? _map;
    // метод, замещающий маппинг с целью оптимизации
    private Func<QueryResult<T>>? _replacer;
    private List<Column> _columns;
    public override JoinSection PathTo { get; protected set; }
    public override IReadOnlyCollection<Column> Columns
    {
        get => _columns;
    }
    public Mapper(Func<NpgsqlDataReader, QueryResult<T>> map, IReadOnlyCollection<Column> mappedColumns)
    {
        _map = map;
        _replacer = null;
        _columns = mappedColumns.ToList();
        PathTo = new JoinSection();
    }
    public Mapper(Func<QueryResult<T>> replacer)
    {
        _map = null;
        _replacer = replacer;
        _columns = new List<Column>();
        PathTo = new JoinSection();
    }

    public QueryResult<T> Map(NpgsqlDataReader reader)
    {
        if (_replacer is not null)
        {
            return _replacer.Invoke();
        }
        return _map.Invoke(reader);
    }

    public string AsSQLText(Column distinctOn)
    {
        return "SELECT " + "DISTINCT ON (" + distinctOn.AsSQLText() + ") " + string.Join(", ", _columns.Select(x => x.AsSQLText()));
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

public abstract class Mapper
{

    public virtual JoinSection PathTo { get; protected set; }
    public virtual IReadOnlyCollection<Column> Columns { get; }
    public abstract void AssumeChild(Mapper mapper);

}

