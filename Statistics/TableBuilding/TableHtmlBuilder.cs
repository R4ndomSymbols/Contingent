using System.Text;

namespace StudentTracking.Statistics;

public class HtmlTableBuilder {

    private StatisticTable _table;
    public HtmlTableBuilder(StatisticTable table){
        _table = table;
    }

    public string Build(){
        var mainBuilt = new StringBuilder();
        _table


    }

    protected class HtmlTableHeader {
   
    protected class HtmlTableBody {


    }




}


public class HtmlTableCell {

    public CellPlacement Placement {get; set;}
    public string Content {get; set;}

    public HtmlTableCell(){

    }

}

