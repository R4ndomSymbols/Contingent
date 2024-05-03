namespace Tests;

public class NamedList : IDataSource
{
    private readonly string _name;
    public string Value => Pick();
    public string Name => _name;
    public readonly IList<string> Data;
    public NamedList(string name, List<string> data)
    {
        _name = name;
        Data = data;
    }
    public string Pick()
    {
        return RandomPicker<string>.Pick(Data);
    }
}
