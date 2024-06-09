using Npgsql;
using Contingent.Models.Domain.Orders;
using Contingent.SQL;
using Contingent.Utilities;
using Contingent.Models.Domain.Students;
using Contingent.Models.Domain.Groups;


namespace Contingent.Models.Domain.Flow;

// общие запросы по таблице истории студентов
public static class FlowHistory
{

    private static SelectQuery<DateTime> GetLastOrderDateSubquery(string innerFlowTableName)
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
            new Column("order_id", innerFlowTableName),
            new Column("id", "orders")
        );
        var where = new ComplexWhereCondition(
            new WhereCondition(
                new Column("student_id", innerFlowTableName),
                new Column("student_id", "student_flow"),
                WhereCondition.Relations.Equal
            )
        );
        var orderBy = new OrderByCondition(
            new Column("effective_date", "orders"),
            OrderByCondition.OrderByTypes.DESC
        );
        orderBy.AddColumn(new Column("creation_timestamp", "orders"), OrderByCondition.OrderByTypes.DESC);
        var query = SelectQuery<DateTime>.Init("student_flow", innerFlowTableName)
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
        string alias = "stdflow";

        if (queryParameters is null)
        {
            throw new Exception("Параметры запроса должны быть указаны");
        }

        Mapper<StudentModel> studentMapper;
        if (queryParameters.ExtractByStudent is not null)
        {
            studentMapper = new Mapper<StudentModel>(
                () => QueryResult<StudentModel>.Found(queryParameters.ExtractByStudent)
            );
            studentMapper.PathTo.AppendJoin(
                queryParameters.IncludeNotRegisteredStudents ? JoinSection.JoinType.FullJoin : JoinSection.JoinType.LeftJoin,
                new Column("student_id", "student_flow"),
                new Column("id", "students")
            );
        }
        else if (queryParameters.ExtractStudents)
        {
            studentMapper = StudentModel.GetMapper(
            queryParameters.ExtractAddress,
            new Column("student_id", "student_flow"),
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
            groupMapper = GroupModel.GetMapper(new Column("group_id_to", "student_flow"), JoinSection.JoinType.LeftJoin);
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
            new Column("order_id", "student_flow"),
            new Column("id", "orders")
            );
        }
        else if (queryParameters.ExtractOrders)
        {
            orderMapper = Order.GetMapper(new Column("order_id", "student_flow"), JoinSection.JoinType.LeftJoin);
        }
        else
        {
            orderMapper = new Mapper<Order>(
                () => QueryResult<Order>.NotFound()
            );
            orderMapper.PathTo.AppendJoin(
            JoinSection.JoinType.LeftJoin,
            new Column("order_id", "student_flow"),
            new Column("id", "orders")
            );
        }



        ComplexWhereCondition basicFilter = ComplexWhereCondition.Empty;
        var sqlParameters = parameters ?? new SQLParameterCollection();
        if (queryParameters.ExtractAbsoluteLastState)
        {
            basicFilter = basicFilter.Unite(ComplexWhereCondition.ConditionRelation.AND,
             new ComplexWhereCondition(
            new WhereCondition(
                new Column("creation_timestamp", "orders"),
                GetLastOrderDateSubquery(alias),
                WhereCondition.Relations.Equal
            ),
            new WhereCondition(
                new Column("order_id", "student_flow"), WhereCondition.Relations.Is
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
            var p1 = sqlParameters.Add((int)queryParameters.ExtractByStudent.Id!);
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
                    new Column("group_id_to", "student_flow"),
                    p1,
                    WhereCondition.Relations.Equal
                )
            );
            basicFilter = basicFilter.Unite(ComplexWhereCondition.ConditionRelation.AND, filter);
        }
        basicFilter = basicFilter.Unite(ComplexWhereCondition.ConditionRelation.AND, additionalFilter);

        var historyMapper = StudentFlowRecord.GetMapper(
            orderMapper, groupMapper, studentMapper
        );

        var query =
        // последнее состояние исключает неуникальность студентов
        (queryParameters.ExtractStudentUnique ?
        SelectQuery<StudentFlowRecord>.Init("student_flow", new Column("id", "students"))
        : SelectQuery<StudentFlowRecord>.Init("student_flow"))
        .AddMapper(historyMapper)
        .AddJoins(historyMapper.PathTo)
        .AddWhereStatement(basicFilter)
        .AddOrderByStatement(
            queryParameters.OverallHistorical ?
            new OrderByCondition(
                new Column("id", "student_flow"),
                OrderByCondition.OrderByTypes.DESC, false
            ) : null
        )
        .AddParameters(sqlParameters)
        .Finish().ResultObject;
        using var conn = await Utils.GetAndOpenConnectionFactory();
        return await query.Execute(conn, limits, queryParameters.Transaction, queryParameters.SuppressLogs);
    }

    public static IEnumerable<StudentFlowRecord> GetRecordsByFilter(QueryLimits limits, HistoryExtractSettings settings, ComplexWhereCondition? specific = null, SQLParameterCollection? parameters = null)
    {
        return GetHistoryAggregate(limits: limits, queryParameters: settings, additionalFilter: specific, parameters: parameters).Result;
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
    OnlyExcluded = 0,
    OnlyIncluded = 1,
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
    // последнее состояние студента
    public bool ExtractAbsoluteLastState
    {
        get => _extractLastState;
        set
        {
            _extractLastState = value;
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
                // невозможно извлечь незарегистрированных студентов
                // т.к. их никогда не будет в приказе
                IncludeNotRegisteredStudents = false;
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
                // тоже самое, в группе всегда зарегистрированный студент
                IncludeNotRegisteredStudents = false;
                ExtractGroups = false;
            }
            _extractByGroup = value;
        }
    }
    // отвечает за порядок выдачи, исторический или случайный
    public bool OverallHistorical
    {
        get => _overallHistorical;
        set
        {
            _overallHistorical = value;
        }
    }

    public ObservableTransaction? Transaction { get; set; }
    public bool SuppressLogs;
    public HistoryExtractSettings()
    {
        SuppressLogs = false;
        _extractLastState = false;
        _extractStudentUnique = false;
        ExtractByOrder = null;
        ExtractAddress = (false, false);
        ExtractOrders = false;
        ExtractByStudent = null;
        IncludeNotRegisteredStudents = false;
    }

}
