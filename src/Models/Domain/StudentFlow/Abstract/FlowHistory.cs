using Npgsql;
using Contingent.Models.Domain.Orders;
using Contingent.SQL;
using Utilities;
using Contingent.Models.Domain.Students;
using Contingent.Models.Domain.Groups;


namespace Contingent.Models.Domain.Flow;

// общие запросы по таблице истории студентов
public static class FlowHistory
{
    public static DateTime CurrentPeriodStartDate
    {
        get
        {
            var now = DateTime.Now;
            return new DateTime(now.Year - 1, 10, 1);
        }
    }
    public static DateTime CurrentPeriodEndDate
    {
        get
        {
            var now = DateTime.Now;
            return new DateTime(now.Year, 9, 30);
        }
    }

    private static SelectQuery<DateTime> GetLastOrderDateSubquery(string outerTableName)
    {
        var mapper = new Mapper<DateTime>(
            (r) =>
            {
                if (r["max_date"].GetType() == typeof(DBNull))
                {
                    return QueryResult<DateTime>.NotFound();
                }
                else
                {
                    return QueryResult<DateTime>.Found((DateTime)r["max_date"]);
                }
            },
            new Column[] { new Column("creation_timestamp", "max_date", "orders") }
        );
        var joins = new JoinSection().AppendJoin(
            JoinSection.JoinType.InnerJoin,
            new Column("id", "orders"),
            new Column("order_id", "student_flow")

        );
        var where = new ComplexWhereCondition(
            new WhereCondition(
                new Column("student_id", "student_flow"),
                new Column("student_id", outerTableName),
                WhereCondition.Relations.Equal
            )
        );
        var orderBy = new OrderByCondition(
            new Column("effective_date", "orders"),
            OrderByCondition.OrderByTypes.DESC
        );
        orderBy.AddColumn(new Column("creation_timestamp", "orders"), OrderByCondition.OrderByTypes.DESC);
        var query = SelectQuery<DateTime>.Init("orders")
        .AddJoins(joins)
        .AddMapper(mapper)
        .AddOrderByStatement(orderBy)
        .AddLimits(new QueryLimits(0, 1))
        .AddWhereStatement(where).Finish();
        return query.ResultObject;
    }
    private static async Task<IEnumerable<StudentFlowRecord>> GetHistoryAggregate(
        QueryLimits limits,
        HistoryExtractSettings queryParameters,
        ComplexWhereCondition? additionalFilter = null,
        SQLParameterCollection? parameters = null,
        DateTime? before = null
        )
    {
        if (queryParameters is null)
        {
            throw new Exception("Параметры запроса должны быть указаны");
        }
        string alias = "outer_table";

        Mapper<StudentModel> studentMapper;
        if (queryParameters.ExtractByStudent is not null)
        {
            studentMapper = new Mapper<StudentModel>(
                () => QueryResult<StudentModel>.Found(queryParameters.ExtractByStudent)
            );
            studentMapper.PathTo.AppendJoin(
                queryParameters.IncludeNotRegisteredStudents ? JoinSection.JoinType.FullJoin : JoinSection.JoinType.LeftJoin,
                new Column("student_id", alias),
                new Column("id", "students")
            );
        }
        else if (queryParameters.ExtractStudents)
        {
            studentMapper = StudentModel.GetMapper(
            queryParameters.ExtractAddress,
            new Column("student_id", alias),
            queryParameters.IncludeNotRegisteredStudents ? JoinSection.JoinType.FullJoin : JoinSection.JoinType.LeftJoin);
        }
        else
        {
            studentMapper = new Mapper<StudentModel>(
                () => QueryResult<StudentModel>.NotFound()
            );
        }

        Mapper<GroupModel> groupMapper;
        if (queryParameters.ExtractByGroup is not null)
        {
            groupMapper = new Mapper<GroupModel>(
                () => QueryResult<GroupModel>.Found(queryParameters.ExtractByGroup)
            );
        }
        else if (queryParameters.ExtractGroups)
        {
            groupMapper = GroupModel.GetMapper(new Column("group_id_to", alias), JoinSection.JoinType.LeftJoin);
        }
        else
        {
            groupMapper = new Mapper<GroupModel>(
                () => QueryResult<GroupModel>.NotFound()
            );
        }

        Mapper<Order> orderMapper;
        if (queryParameters.ExtractByOrder is not null && queryParameters.ExtractByOrder.Value.mode == OrderRelationMode.OnlyIncluded)
        {
            orderMapper = new Mapper<Order>(
                () => QueryResult<Order>.Found(queryParameters.ExtractByOrder.Value.model)
            );
            orderMapper.PathTo.AppendJoin(
            JoinSection.JoinType.LeftJoin,
            new Column("order_id", alias),
            new Column("id", "orders")
            );
        }
        else if (queryParameters.ExtractOrders)
        {
            orderMapper = Order.GetMapper(new Column("order_id", alias), JoinSection.JoinType.LeftJoin);
        }
        else
        {
            orderMapper = new Mapper<Order>(
                () => QueryResult<Order>.NotFound()
            );
            orderMapper.PathTo.AppendJoin(
            JoinSection.JoinType.LeftJoin,
            new Column("order_id", alias),
            new Column("id", "orders")
            );
        }

        var usedCols = new List<Column> {
            new Column("id", "rec_id", alias),
            new Column("student_id", alias),
            new Column("group_id_to", alias),
            new Column("order_id", alias)
        };

        var historyMapper = new Mapper<StudentFlowRecord>(
            (m) =>
            {
                var raw = new RawStudentFlowRecord();
                raw.Id = m["rec_id"].GetType() == typeof(DBNull) ? null : (int)m["rec_id"];
                raw.StudentId = m["student_id"].GetType() == typeof(DBNull) ? null : (int)m["student_id"];
                raw.GroupToId = m["group_id_to"].GetType() == typeof(DBNull) ? null : (int)m["group_id_to"];
                raw.OrderId = m["order_id"].GetType() == typeof(DBNull) ? null : (int)m["order_id"];
                var studentResult = studentMapper.Map(m);
                var groupResult = groupMapper.Map(m);
                var orderResult = orderMapper.Map(m);
                var group = groupResult.IsFound ? groupResult.ResultObject : null;
                var student = studentResult.IsFound ? studentResult.ResultObject : null;
                var order = orderResult.IsFound ? orderResult.ResultObject : null;
                return QueryResult<StudentFlowRecord>.Found(new StudentFlowRecord(raw, order, student, group));
            },
            usedCols
        );
        historyMapper.AssumeChild(studentMapper);
        historyMapper.AssumeChild(groupMapper);
        historyMapper.AssumeChild(orderMapper);

        ComplexWhereCondition basicFilter = ComplexWhereCondition.Empty;
        var sqlParameters = parameters ?? new SQLParameterCollection();
        if (queryParameters.ExtractLastState)
        {
            basicFilter = basicFilter.Unite(ComplexWhereCondition.ConditionRelation.OR,
             new ComplexWhereCondition(
            new WhereCondition(
                new Column("creation_timestamp", "orders"),
                GetLastOrderDateSubquery(alias),
                WhereCondition.Relations.Equal
            ),
            new WhereCondition(
                new Column("order_id", alias), WhereCondition.Relations.Is
            ),
            ComplexWhereCondition.ConditionRelation.OR, true
            ), false);

        };
        if (queryParameters.ExtractByOrder is not null)
        {
            var p1 = sqlParameters.Add(queryParameters.ExtractByOrder.Value.model.Id);
            ComplexWhereCondition? filter = null;
            switch (queryParameters.ExtractByOrder.Value.mode)
            {
                case OrderRelationMode.OnlyIncluded:
                    filter = new ComplexWhereCondition(
                    new WhereCondition(
                        new Column("id", "orders"),
                        p1,
                        WhereCondition.Relations.Equal
                    )
                );
                    break;
                case OrderRelationMode.OnlyExcluded:
                    filter = new ComplexWhereCondition(
                    new WhereCondition(
                        new Column("id", "orders"),
                        p1,
                        WhereCondition.Relations.NotEqual
                    ),
                    new WhereCondition(
                        new Column("id", "orders"),
                        WhereCondition.Relations.Is
                    ),
                    ComplexWhereCondition.ConditionRelation.OR,
                    true
                );
                    break;
            }
            basicFilter = basicFilter.Unite(ComplexWhereCondition.ConditionRelation.AND, filter);
        }
        if (queryParameters.ExtractByStudent is not null)
        {
            var p1 = sqlParameters.Add<int>((int)queryParameters.ExtractByStudent.Id);
            ComplexWhereCondition? filter = new ComplexWhereCondition(
                new WhereCondition(
                    new Column("id", "students"),
                    p1,
                    WhereCondition.Relations.Equal
                )
            );
            basicFilter = basicFilter.Unite(ComplexWhereCondition.ConditionRelation.AND, filter);
        }
        if (queryParameters.ExtractByGroup is not null)
        {
            var p1 = sqlParameters.Add<int>((int)queryParameters.ExtractByGroup.Id);
            ComplexWhereCondition? filter = new ComplexWhereCondition(
                new WhereCondition(
                    new Column("group_id_to", alias),
                    p1,
                    WhereCondition.Relations.Equal
                )
            );
            basicFilter = basicFilter.Unite(ComplexWhereCondition.ConditionRelation.AND, filter);
        }
        basicFilter.Unite(ComplexWhereCondition.ConditionRelation.AND, additionalFilter);

        var query =
        // последнее состояние исключает неуникальность студентов
        (queryParameters.ExtractStudentUnique ?
        SelectQuery<StudentFlowRecord>.Init("student_flow", new Column("id", "students"), alias)
        : SelectQuery<StudentFlowRecord>.Init("student_flow", alias))
        .AddMapper(historyMapper)
        .AddJoins(historyMapper.PathTo)
        .AddWhereStatement(basicFilter)
        .AddOrderByStatement(
            queryParameters.OverallHistorical ?
            new OrderByCondition(
                new Column("id", alias),
                OrderByCondition.OrderByTypes.DESC
            ) : queryParameters.ExtractStudentUnique ?
            new OrderByCondition(
                new Column("id", "students"),
                OrderByCondition.OrderByTypes.ASC
            ) : null
        )
        .AddParameters(sqlParameters)
        .Finish().ResultObject;
        using var conn = await Utils.GetAndOpenConnectionFactory();
        return await query.Execute(conn, limits);
    }

    public static IEnumerable<StudentFlowRecord> GetRecordsByFilter(QueryLimits limits, HistoryExtractSettings settings)
    {
        return GetHistoryAggregate(limits: limits, queryParameters: settings).Result;
    }

    public static void DeleteRecords(IEnumerable<int> ids)
    {
        if (!ids.Any())
        {
            return;
        }
        using var conn = Utils.GetAndOpenConnectionFactory().Result;
        string cmdText = "DELETE FROM student_flow WHERE id = ANY(@p1)";
        var cmd = new NpgsqlCommand(cmdText, conn);
        var p = new NpgsqlParameter();
        p.ParameterName = "p1";
        p.Value = ids.ToArray();
        p.NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Array | NpgsqlTypes.NpgsqlDbType.Integer;
        cmd.Parameters.Add(p);
        using (cmd)
        {
            cmd.ExecuteNonQuery();
        }
    }
}

public enum OrderRelationMode
{
    OnlyExcluded,
    OnlyIncluded
}

public class HistoryExtractSettings
{
    private bool _extractLastState;
    private bool _extractStudentUnique;
    private (Order model, OrderRelationMode mode)? _extractByOrder;
    private StudentModel? _extractByStudent;
    private GroupModel? _extractByGroup;
    private bool _extractOrders;
    private bool _extractGroups;
    private bool _extractStudent;
    private bool _overallHistorical;

    public bool ExtractLastState
    {
        get => _extractLastState;
        set
        {
            if (value)
            {
                if (ExtractByOrder is not null)
                {
                    if (ExtractByOrder.Value.mode == OrderRelationMode.OnlyIncluded)
                    {
                        ExtractByOrder = null;
                    }
                    ExtractByGroup = null;
                }
                _extractStudentUnique = false;
                _extractLastState = value;
            }
            else
            {
                _extractLastState = false;
            }
        }
    }
    public bool ExtractStudentUnique
    {
        get => _extractStudentUnique;
        set
        {
            _extractStudentUnique = value;
        }

    }

    public (bool actual, bool legal) ExtractAddress { get; set; }

    public bool ExtractOrders
    {
        get => _extractOrders;
        set
        {
            if (value)
            {
                ExtractByOrder = null;
            }
            _extractOrders = value;
        }
    }
    public bool ExtractGroups
    {
        get => _extractGroups;
        set
        {
            if (value)
            {
                ExtractByGroup = null;
            }
            _extractGroups = value;
        }
    }
    public bool ExtractStudents
    {
        get => _extractStudent;
        set
        {
            if (value)
            {
                ExtractByStudent = null;
            }
            _extractStudent = value;
        }
    }
    public bool IncludeNotRegisteredStudents { get; set; }

    public StudentModel? ExtractByStudent
    {
        get => _extractByStudent;
        set
        {
            if (value is not null)
            {
                ExtractStudents = false;
            }
            _extractByStudent = value;
        }
    }
    public (Order model, OrderRelationMode mode)? ExtractByOrder
    {
        get => _extractByOrder;
        set
        {
            if (value is not null)
            {
                IncludeNotRegisteredStudents = value.Value.mode == OrderRelationMode.OnlyExcluded;
                ExtractStudentUnique = true;
                ExtractOrders = false;
            }
            _extractByOrder = value;
        }
    }
    public GroupModel? ExtractByGroup
    {
        get => _extractByGroup;
        set
        {
            if (value is not null)
            {
                ExtractGroups = false;
            }
            _extractByGroup = value;
        }
    }
    public bool OverallHistorical
    {
        get => _overallHistorical;
        set
        {
            if (value)
            {
                ExtractStudentUnique = false;
            }
            _overallHistorical = value;
        }
    }
    public HistoryExtractSettings()
    {
        _extractLastState = false;
        _extractStudentUnique = false;
        ExtractByOrder = null;
        ExtractAddress = (false, false);
        ExtractOrders = false;
        ExtractByStudent = null;
        IncludeNotRegisteredStudents = false;
    }

}
