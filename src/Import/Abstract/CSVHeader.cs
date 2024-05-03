namespace Contingent.Import;

public class CSVHeader
{

    public CSVHeader()
    {
        _columns = new Dictionary<string, int>();
    }

    private Dictionary<string, int> _columns;
    public int ColumnCount => _columns.Count;
    public void AddColumn(int position, string name)
    {
        _columns.Add(name.ToLower(), position);
    }
    public int this[string name]
    {
        get
        {
            if (_columns.TryGetValue(name.ToLower(), out int pos))
            {
                return pos;
            }
            return -1;
        }
    }

    public override string ToString()
    {
        return string.Join("\n", _columns.Select(x => x.Key + " " + x.Value));
    }




}
