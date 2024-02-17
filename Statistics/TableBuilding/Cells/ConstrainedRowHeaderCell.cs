using StudentTracking.SQL;
namespace StudentTracking.Statistics;

public class ConstrainedRowHeaderCell {

    private ConstrainedRowHeaderCell _aggregateRoot;
    private List<ConstrainedRowHeaderCell> _toAggregate;
    public ComplexWhereCondition? Constraint {get; set;}
    public string Name {get; set; }
    public int Y;
    public int X => 1; 
    public bool IsOnlyHeader {get; private init;}
    // кусок агрегата
    public ConstrainedRowHeaderCell(string name, ComplexWhereCondition constraint, ConstrainedRowHeaderCell aggregateRoot){
        Constraint = constraint;
        Name = name;
        IsOnlyHeader = false;
        _aggregateRoot = aggregateRoot;
    }
    // заголовок
    public ConstrainedRowHeaderCell(string name){
        Constraint = null;
        Name = name;
        _toAggregate = null; 
        IsOnlyHeader = true;
    }
    // агрегат, не поддерживается на данный момент
    public ConstrainedRowHeaderCell(string name, IEnumerable<ConstrainedRowHeaderCell> aggregate){
        Constraint = null;
        Name = name;
        IsOnlyHeader = false;
        _toAggregate = new List<ConstrainedRowHeaderCell>();
        _toAggregate.AddRange(aggregate);        
    }
    

    public void Link(ConstrainedRowHeaderCell aggregateParticipant){
        _toAggregate.Add(aggregateParticipant);
    }
}

