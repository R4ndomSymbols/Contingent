using System.Text;
using Npgsql;
using Utilities;

namespace StudentTracking.SQL;

public abstract class SelectQuery : IQueryPart
{
    public abstract string AsSQLText();
}


public class SelectQuery<T> : SelectQuery
{
    private Column? _distictOn;
    private SQLParameterCollection? _parameters;
    private Mapper<T> _mapper;
    private JoinSection? _joins;
    private ComplexWhereCondition? _whereClause;
    private OrderByCondition? _orderBy;
    private QueryLimits? _predefinedLimits;
    private string _sourceTable;
    private bool _finished;

    private SelectQuery()
    {
        _finished = false;
        _distictOn = null;
    }
    private SelectQuery(Column distinctOn) : this()
    {
        _distictOn = distinctOn;
    }
    private SelectQuery(string name, string alias = "")
    {
        if (alias != "")
        {
            _sourceTable = name + " AS " + alias;
        }
        else
        {
            _sourceTable = name;
        }
    }

    public static SelectQuery<T> Init(string sourceTable)
    {
        var tmp = new SelectQuery<T>(sourceTable);
        return tmp;
    }
    public static SelectQuery<T> Init(string sourceTable, string alias)
    {
        var tmp = new SelectQuery<T>(sourceTable, alias);
        return tmp;
    }
    public static SelectQuery<T> Init(string sourceTable, Column distinctOnColumn, string alias = "")
    {
        var tmp = new SelectQuery<T>(sourceTable, alias);
        if (alias != "")
        {
            tmp._sourceTable = sourceTable + " AS " + alias;
        }
        else
        {
            tmp._sourceTable = sourceTable;
        }
        tmp._distictOn = distinctOnColumn;
        return tmp;
    }
    public SelectQuery<T> AddMapper(Mapper<T> mapper)
    {
        _mapper = mapper;
        return this;
    }
    public SelectQuery<T> AddJoins(JoinSection? joins)
    {
        _joins = joins;
        return this;
    }
    public SelectQuery<T> AddWhereStatement(ComplexWhereCondition? condition)
    {
        _whereClause = condition;
        return this;
    }
    public SelectQuery<T> AddOrderByStatement(OrderByCondition? orderBy)
    {
        _orderBy = orderBy;
        return this;
    }
    public SelectQuery<T> AddParameters(SQLParameterCollection? parameters)
    {
        _parameters = parameters;
        return this;
    }
    public SelectQuery<T> AddLimits(QueryLimits? queryLimits)
    {
        _predefinedLimits = queryLimits;
        return this;
    }

    public Result<SelectQuery<T>> Finish()
    {
        if (_mapper == null)
        {
            return Result<SelectQuery<T>>.Failure(new ValidationError(nameof(_mapper), "О"));
        }
        if (_parameters == null)
        {
            _parameters = new SQLParameterCollection();
        }
        _finished = true;
        return Result<SelectQuery<T>>.Success(this);
    }
    public override string AsSQLText()
    {
        if (!_finished)
        {
            throw new Exception("Невозможно преобразовать несформированный запрос");
        }
        StringBuilder queryBuilder = new StringBuilder();
        if (_distictOn is null)
        {
            queryBuilder.Append(_mapper.AsSQLText() + "\n");
        }
        else
        {
            queryBuilder.Append(_mapper.AsSQLText(_distictOn) + "\n");
        }

        queryBuilder.Append("FROM " + _sourceTable + "\n");
        if (_joins != null)
        {
            queryBuilder.Append(_joins.AsSQLText() + "\n");
        }
        if (_whereClause != null)
        {
            queryBuilder.Append(_whereClause.AsSQLText() + "\n");
        }
        if (_orderBy != null)
        {
            queryBuilder.Append(_orderBy.AsSQLText() + "\n");
        }
        if (_predefinedLimits is not null)
        {
            if (!_predefinedLimits.IsUnlimited)
            {
                queryBuilder.Append(" LIMIT " + _predefinedLimits.PageLength + " ");
                if (_predefinedLimits.GlobalOffset != 0)
                {
                    queryBuilder.Append( " OFFSET " + _predefinedLimits.GlobalOffset);
                }
                else
                {
                    queryBuilder.Append(" OFFSET " + _predefinedLimits.PageSkipCount * _predefinedLimits.PageLength);
                }
            }
        }

        return queryBuilder.ToString();
    }

    public async Task<IReadOnlyCollection<T>> Execute(NpgsqlConnection conn, QueryLimits limits, ObservableTransaction? scope = null)
    {
        if (!_finished)
        {
            throw new Exception("неполный SQL запрос");
        }
        var cmdText = AsSQLText();
        if (!limits.IsUnlimited)
        {
            cmdText += " LIMIT " + limits.PageLength + " ";
            cmdText += " OFFSET " + limits.GlobalOffset;
        }

        // логирование
        Console.WriteLine(cmdText);
        NpgsqlCommand cmd;
        if (scope is null)
        {
            cmd = new NpgsqlCommand(cmdText, conn);
        }
        else
        {
            cmd = new NpgsqlCommand(cmdText, scope.Connection, scope.Transaction);
        }
        if (_parameters != null)
        {
            foreach (var c in _parameters)
            {
                cmd.Parameters.Add(c.ToNpgsqlParameter());
            }
        }
        await using (cmd)
        {
            var result = new List<T>();
            await using var reader = await cmd.ExecuteReaderAsync();
            if (!reader.HasRows)
            {
                return result;
            }

            while (reader.Read())
            {
                var built = _mapper.Map(reader);
                if (built.IsFound)
                {
                    result.Add(built.ResultObject);
                }

            }
            return result;
        }
    }
}

public class QueryLimits
{
    public static QueryLimits Unlimited => new QueryLimits() { IsUnlimited = true };
    public bool IsUnlimited { get; private init; }
    public readonly int PageSkipCount;
    public readonly int PageLength;
    public int GlobalOffset => _preciseOffset == 0 ? PageSkipCount * PageLength : _preciseOffset;
    private int _preciseOffset;

    public QueryLimits(int pageSkipCount, int pageLength, int preciseOffset = 0)
    {
        _preciseOffset = preciseOffset;
        PageSkipCount = pageSkipCount;
        PageLength = pageLength;
        IsUnlimited = false;
    }
    public QueryLimits()
    {
        IsUnlimited = true;
    }

}


