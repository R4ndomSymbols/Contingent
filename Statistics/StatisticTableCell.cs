namespace StudentTracking.Statistics;

public class StatisticTableCell {

    public int X {get; set;}
    public int Y {get; set;}
    private Func<Task<int>> _statsGetter;
    public StatisticTableCell(Func<Task<int>> statsGetter, int x, int y, ){
        _statsGetter = statsGetter;
    }
    public async Task<int> GetStats(){
        return await _statsGetter.Invoke();
    }

}
