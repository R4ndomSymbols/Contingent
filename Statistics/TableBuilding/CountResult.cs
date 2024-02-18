namespace StudentTracking.Statistics;

public class CountResult {

    public int Count {get; set; }
    public CountResult(){
        Count = 0;
    }
    public CountResult(int count){
        Count = count;
    }

    public override string ToString()
    {
        return Count == 0 ? "X" : Count.ToString();
    }
}
