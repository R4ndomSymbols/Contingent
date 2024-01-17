using System.Text;
using Microsoft.AspNetCore.Identity;
using Npgsql;

namespace StudentTracking.Models.SQL;


public class SelectQuery<T> {

    private SQLParameters _parameters;
    private Mapper<T> _mapper;
    private JoinSection? _joins;
    private ComplexWhereCondition _whereClause;
    private string _sourceTable;

    public SelectQuery(string sourceTable, SQLParameters par, Mapper<T> mapper, JoinSection? joins, ComplexWhereCondition statement){
        _parameters = par;
        _mapper = mapper;
        _joins = joins;
        _whereClause = statement;
        _sourceTable = sourceTable; 
    }

    private string ToQuery(){
        StringBuilder queryBuilder = new StringBuilder();
        queryBuilder.Append("SELECT ");
        queryBuilder.Append(string.Join(", ", _mapper.GetSelectedColumns()));
        queryBuilder.Append("\nFROM " +  _sourceTable + "\n");
        if (_joins!=null){
            queryBuilder.Append(_joins.ToString());
        }
        queryBuilder.Append("\n");
        queryBuilder.Append("WHERE " + _whereClause.ToString());
        return queryBuilder.ToString();
    }

    public async Task<List<T>?> Execute(NpgsqlConnection conn, int maxRows, Func<T> newObjectGetter)
    {   
        var cmdText = ToQuery();
        Console.WriteLine(cmdText);
        var cmd = new NpgsqlCommand(cmdText, conn);
        foreach (var c in _parameters){
            cmd.Parameters.Add(c);
        }
        await using (cmd){
            await using var reader = await cmd.ExecuteReaderAsync();
            if (!reader.HasRows){
                return null;
            }
            var result = new List<T>();
            while(reader.Read() && result.Count < maxRows){
                var built = newObjectGetter.Invoke();
                _mapper.Map(built, reader);
                result.Add(built);
            }
            return result; 
        }   
    }
}