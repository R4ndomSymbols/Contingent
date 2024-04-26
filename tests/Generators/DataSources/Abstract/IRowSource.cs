namespace Tests;

public interface IRowSource {
    public string? GetHeader(int pos);
    public string GetData(int pos);
    public void UpdateState();
    public int ColumnCount {get;}
}