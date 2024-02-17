namespace StudentTracking.SQL;


public class ForeignKey
{
    public string TableName {get; set; }
    public string ColumnName {get; set;}
    public ForeignKey Reference  {get; set;}

}

public class PrimaryKey {

    public string TableName {get; set; }
    public string ColumnName {get; set;}

}

