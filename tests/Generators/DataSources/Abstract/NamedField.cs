namespace Tests;

public class NamedField : IDataSource {

    private readonly Func<string> _gen;
    private string _name;
    public string Name => _name;
    public string Value => _gen.Invoke();
    public NamedField(string name, Func<string> source)
    {
        _name = name;
        _gen = source;
    }

}