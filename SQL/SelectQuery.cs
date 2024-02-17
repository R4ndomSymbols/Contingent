using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Npgsql;
using Utilities;

namespace StudentTracking.SQL;


public class SelectQuery<T>
{
    private Column? _distictOn;
    private SQLParameterCollection? _parameters;
    private Mapper<T> _mapper;
    private JoinSection? _joins;
    private ComplexWhereCondition? _whereClause;
    private OrderByCondition? _orderBy;
    private string _sourceTable;
    private bool _finished;

    private SelectQuery()
    {
        _finished = false;
        _distictOn = null;
    }
    private SelectQuery(Column distinctOn) : this() {
        _distictOn = distinctOn; 
    }

    public static SelectQuery<T> Init(string sourceTable)
    {
        var tmp = new SelectQuery<T>();
        tmp._sourceTable = sourceTable;
        return tmp;
    }
    public static SelectQuery<T> Init(string sourceTable, Column distinctOnColumn)
    {
        var tmp = new SelectQuery<T>(distinctOnColumn);
        tmp._sourceTable = sourceTable;
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

    public Result<SelectQuery<T>?> Finish()
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
    private string ToQuery()
    {
        StringBuilder queryBuilder = new StringBuilder();
        if (_distictOn is null){
            queryBuilder.Append(_mapper.AsSQLText() + "\n");
        }
        else {
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
        return queryBuilder.ToString();
    }

    public async Task<IReadOnlyCollection<T>> Execute(NpgsqlConnection conn, QueryLimits limits)
    {
        if (!_finished)
        {
            throw new Exception("неполный SQL запрос");
        }
        var cmdText = ToQuery();
        cmdText += "LIMIT " + limits.PageLength;
        // логирование
        Console.WriteLine(cmdText);
        var cmd = new NpgsqlCommand(cmdText, conn);
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
                var built = await _mapper.Map(reader);
                if (built.IsFound){
                    result.Add(built.ResultObject);
                }
                
            }
            return result;
        }
    }
}

public class QueryLimits
{

    public readonly int PageSkipCount;
    public readonly int PageLength;

    public QueryLimits(int pageSkipCount, int pageLength)
    {
        PageSkipCount = pageSkipCount;
        PageLength = pageLength;
    }

}
