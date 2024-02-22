namespace StudentTracking.Statistics;

public class StatisticTableCell {

    public int X {get; set;}
    public int Y {get; set;}
    public Func<CountResult> StatsGetter {get; set; }
    public StatisticTableCell(int x, int y){
        X = x;
        Y = y;
        StatsGetter = null;
    }
    public CountResult GetStats(){
        if (StatsGetter is null){
            throw new Exception("Не определен способ получения статистики для клетки");
        }
        return StatsGetter.Invoke();
    }

}
