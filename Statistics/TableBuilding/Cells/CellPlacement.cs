using System.IO.Compression;

namespace StudentTracking.Statistics;
public struct CellPlacement {
    public int X {get; set;}
    public int Y {get; set;}
    public int RowSpan {get; set; }
    public int ColumnSpan {get; set; }

    public CellPlacement(){

        X = 0;
        Y = 0;
        RowSpan = 1;
        ColumnSpan = 1;
    }

    public CellPlacement (int x, int y, int rowSpan, int columnSpan){
        X = x;
        Y = y;
        RowSpan = rowSpan;
        ColumnSpan = columnSpan;
    } 

    public CellPlacement Shift(int x, int y){
        return new CellPlacement(
            this.X + x,
            this.Y + y,
            this.RowSpan,
            this.ColumnSpan
        );
    }
    public CellPlacement ChangeSize(int rows = 0, int cols = 0){
        return new CellPlacement(
            this.X,
            this.Y,
            this.RowSpan + rows,
            this.ColumnSpan + cols
        );
    }
}