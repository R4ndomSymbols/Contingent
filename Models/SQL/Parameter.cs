using System.Collections;
using System.Data.SqlTypes;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using Npgsql;
using NpgsqlTypes;

namespace StudentTracking.Models.SQL;


public class SQLParameterCollection : IEnumerable<SQLParameter>{

    private List<SQLParameter> _parameters;

    public SQLParameterCollection() {
        _parameters = new List<SQLParameter>();
    }
    // возвращает имя параметра в запросе
    public SQLParameter Add<T>(T value){
        string name = GetNextName();
        var result = new SQLParameter<T>(new NpgsqlParameter<T>(name, value));
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
        return _value.ParameterName;
    }

    public override NpgsqlParameter ToNpgsqlParameter()
    {
        return _value;
    }   
}