namespace Contingent.SQL;

// граф отношений в базе данных для вопспроизводства JOIN операций
public class DatabaseGraph
{
    private List<SQLTable> _graph;

    private DatabaseGraph()
    {

        _graph = new List<SQLTable>();

        var studentTable = new SQLTable("students", "id");
        studentTable.AddDataColumns(
            "date_of_birth"
        );


        var specialityTable = new SQLTable("educational_program", "id");
        var groupTable = new SQLTable("educational_group", "group_id");
        var rusCitizenshipTable = new SQLTable("rus_citizenship", "id");
        var ordersTable = new SQLTable("orders", "id");
        var studentFlowTable = new SQLTable("student_flow", "id");

        studentTable.AddForeignKey("rus_citizenship_id", rusCitizenshipTable.Primary);
        groupTable.AddForeignKey("program_id", specialityTable.Primary);
        studentFlowTable.AddForeignKey("student_id", studentTable.Primary);
        studentFlowTable.AddForeignKey("order_id", ordersTable.Primary);
        studentFlowTable.AddForeignKey("group_to_id", groupTable.Primary);

        _graph.AddRange(new List<SQLTable>()
        {studentTable, specialityTable, groupTable,
         rusCitizenshipTable, ordersTable, studentFlowTable});
    }

    public SQLTable? GetByName(string tableName)
    {
        var found = _graph.Where(x => x.TableName == tableName);
        if (found.Any())
        {
            return found.First();
        }
        return null;
    }
    public IEnumerable<SQLTable> GetAllReferencesToPrimaryKey(SQLTable toThis)
    {
        return _graph.Where(t => t.ForeignKeys.Any(key => key.Reference == toThis.Primary));

    }

    private static DatabaseGraph _instance;

    public static DatabaseGraph Instance
    {
        get
        {
            if (_instance is null)
            {
                _instance = new DatabaseGraph();
            }
            return _instance;
        }
    }

}


public class SQLTable
{

    private List<ForeignKey> _outerKeys;
    private List<DataColumn> _dataColumns;
    public string TableName { get; private set; }
    public PrimaryKey Primary { get; private set; }
    public IReadOnlyCollection<ForeignKey> ForeignKeys => _outerKeys.AsReadOnly();
    public IReadOnlyCollection<DataColumn> DataColumns => _dataColumns.AsReadOnly();


    public SQLTable(string name, string primaryKeyColName)
    {
        TableName = name;
        Primary = new PrimaryKey(primaryKeyColName, this);
    }

    public void AddForeignKey(string colName, PrimaryKey referenceTo)
    {
        _outerKeys.Add(new ForeignKey(colName, referenceTo));
    }

    public void AddDataColumns(params string[] names)
    {
        foreach (var str in names)
        {
            _dataColumns.Add(new DataColumn(str));
        }
    }

}



