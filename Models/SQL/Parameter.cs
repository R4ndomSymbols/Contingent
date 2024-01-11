using System.Collections;
using System.Data.SqlTypes;
using System.Runtime.InteropServices;
using Npgsql;
using NpgsqlTypes;

namespace StudentTracking.Models.SQL;


public class SQLParameters : IEnumerable<NpgsqlParameter>{

    private List<NpgsqlParameter> _parameters;

    public SQLParameters() {
        _parameters = new List<NpgsqlParameter>();
    }
    // возвращает имя параметра в запросе
    public string Add(SQLParameter p){
        string name = GetNextName();
        _parameters.Add(p.ToNpgsqlParameter(name));
        return "@"+name;
    }

    public IEnumerator<NpgsqlParameter> GetEnumerator()
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
    
    public abstract NpgsqlParameter ToNpgsqlParameter(string name);
}

public class SQLParameter<T> : SQLParameter {
    
    private T _value;
    public SQLParameter(T value) : base(){
        _value = value;
    }
    public override NpgsqlParameter ToNpgsqlParameter(string name)
    {
        return new NpgsqlParameter<T>(name, _value);
    }   
}