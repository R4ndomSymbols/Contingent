using System.Collections;
using System.Data.SqlTypes;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using Npgsql;
using NpgsqlTypes;

namespace StudentTracking.SQL;


public class SQLParameterCollection : IEnumerable<SQLParameter>{

    private List<SQLParameter> _parameters;

    public SQLParameterCollection() {
        _parameters = new List<SQLParameter>();
    }
    // возвращает параметр в запросе
    public SQLParameter Add<T>(T value){
        string name = GetNextName();
        var result = new SQLParameter<T>(new NpgsqlParameter<T>(name, value));
        _parameters.Add(result);
        return result;
    }
    public SQLParameter Add(object value, NpgsqlDbType type){
        string name = GetNextName();
        var par = new NpgsqlParameter();
        par.NpgsqlDbType = type;
        par.ParameterName = name;
        par.Value = value;
        var result = new BoxedSqlParameter(par);
        _parameters.Add(result);
        return result;
    }

    public IEnumerator<SQLParameter> GetEnumerator()
    {
        return _parameters.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return _parameters.GetEnumerator();
    }

    private string GetNextName(){
        return  "p" + (_parameters.Count + 1).ToString();
    }
} 
public abstract class SQLParameter {
    public bool UseBrackets {get; set;} 
    public abstract NpgsqlParameter ToNpgsqlParameter();
    public abstract string GetName();

}

public class SQLParameter<T> : SQLParameter {
    
    private NpgsqlParameter<T> _value;
    public SQLParameter(NpgsqlParameter<T> value){
        _value = value;
    }

    public override string GetName()
    {
        var n = "@" + _value.ParameterName; 
        if (UseBrackets){
            return "(" + n +")";
        }
        return n;
    }

    public override NpgsqlParameter ToNpgsqlParameter()
    {
        return _value;
    }   
}

public class BoxedSqlParameter : SQLParameter
{
    private NpgsqlParameter _value;
    public BoxedSqlParameter(NpgsqlParameter value){
        _value = value;
    }

    public override string GetName()
    {
        var n = "@" + _value.ParameterName; 
        if (UseBrackets){
            return "(" + n +")";
        }
        return n;
    }

    public override NpgsqlParameter ToNpgsqlParameter()
    {
        return _value;
    }
}