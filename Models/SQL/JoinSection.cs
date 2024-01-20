namespace StudentTracking.Models.SQL;

public class JoinSection : IQueryPart{

    private List<string> _joins;

    public JoinSection(){
        _joins = new List<string>();
    }

    public JoinSection AppendJoin(JoinType type, Column left, Column right){
        string prefix = _joinPrefixes[type];
        _joins.Add(
            prefix + " " + right.TableName + " ON " +  left.ToString() + " = " + right.ToString()
        );
        return this;
    }

    public string AsSQLText()
    {
        return string.Join("\n", _joins);
    }

    public enum JoinType {
        LeftJoin = 1,
        RightJoin = 2,
        FullJoin = 3,
        InnerJoin = 4
    }

    private static readonly Dictionary<JoinType, string> _joinPrefixes = new Dictionary<JoinType, string>{
        {JoinType.FullJoin, "FULL OUTER JOIN"},
        {JoinType.RightJoin, "RIGHT JOIN"},
        {JoinType.LeftJoin, "LEFT JOIN"},
        {JoinType.InnerJoin, "INNER JOIN"}
    };    

}



