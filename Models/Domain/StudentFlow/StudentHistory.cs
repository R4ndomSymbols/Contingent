using System.ComponentModel;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using StudentTracking.Models.Domain.Orders;
using StudentTracking.SQL;
using Utilities;

namespace StudentTracking.Models.Domain.Flow;


public class StudentHistory
{
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
            new Column[] { new Column("MAX", "effective_date", "orders", "max_date") }
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
        var query = SelectQuery<DateTime>.Init("orders")
        .AddJoins(joins)
        .AddMapper(mapper)
        .AddWhereStatement(where).Finish();
        return query.ResultObject;
    }

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
    private List<StudentFlowRecord> _history;
    public IReadOnlyList<StudentFlowRecord> History => _history;
    private StudentHistory()
    {

    }

    public static async Task<StudentHistory> Create(int studentId)
    {
        var result = new StudentHistory();
        result._history = await GetHistory(studentId);
        return result;
    }

    public static async Task<StudentHistory> Create(StudentModel student)
    {
        var result = new StudentHistory();
        result._history = await GetHistory((int)student.Id);
        return result;
    }

    public StudentFlowRecord? GetByOrder(Order orderBy)
    {
        foreach (var rec in _history)
        {
            if (rec.ByOrder.Equals(orderBy))
            {
                return rec;
            }
        }
        return null;
    }

    public StudentFlowRecord? GetClosestBefore(DateTime anchor)
    {
        TimeSpan minDiff = TimeSpan.MaxValue;
        int indexOfClosest = -1;
        for (int i = 0; i < _history.Count; i++)
        {
            var effDate = _history[i].ByOrder.EffectiveDate;
            if (effDate < anchor)
            {
                var diff = anchor - effDate;
                if (diff < minDiff)
                {
                    indexOfClosest = i;
                }
            }
        }
        if (indexOfClosest == -1)
        {
            return null;
        }
        else
        {
            return _history[indexOfClosest];
        }
    }
    public StudentFlowRecord? GetClosestAfter(Order order)
    {
        if (order is null)
        {
            throw new ArgumentNullException(nameof(order));
        }
        return GetClosestAfter(order.EffectiveDate);
    }
    public StudentFlowRecord? GetClosestBefore(Order order)
    {
        if (order is null)
        {
            throw new ArgumentNullException(nameof(order));
        }
        return GetClosestBefore(order.EffectiveDate);
    }
    public StudentFlowRecord? GetClosestAfter(DateTime anchor)
    {
        TimeSpan minDiff = TimeSpan.MaxValue;
        int indexOfClosest = -1;
        for (int i = 0; i < _history.Count; i++)
        {
            var effDate = _history[i].ByOrder.EffectiveDate;
            if (effDate > anchor)
            {
                var diff = anchor - effDate;
                if (diff < minDiff)
                {
                    indexOfClosest = i;
                }
            }
        }
        if (indexOfClosest == -1)
        {
            return null;
        }
        else
        {
            return _history[indexOfClosest];
        }
    }

    public StudentFlowRecord? GetLastRecord()
    {
        StudentFlowRecord? last = null;
        DateTime max = DateTime.MinValue;
        for (int i = 0; i < _history.Count; i++)
        {
            if (_history[i].ByOrder.EffectiveDate > max)
            {
                last = _history[i];
                max = last.ByOrder.EffectiveDate;
            }
        }
        return last;

    }

    // получает всю историю студента в приказах
    // история НЕ отсортирована по дате регистрации приказа
    private static async Task<List<StudentFlowRecord>> GetHistory(int id)
    {
        var sqlParams = new SQLParameterCollection();
        var p1 = sqlParams.Add(id);
        var where = new ComplexWhereCondition(
            new WhereCondition(
                new Column("id", "students"),
                p1,
                WhereCondition.Relations.Equal
            ),
            new WhereCondition(
                new Column("id", "outer_table"),
                WhereCondition.Relations.IsNot
            ),
            ComplexWhereCondition.ConditionRelation.AND

        ); 

        var history = await GetHistoryAggregate(new QueryLimits(0,50), 
        addtionalFilter: where,
        parameters: sqlParams,
        addOrders: true, 
        queryLastOnly: false);
        return history.ToList();
    }

    public static async Task<bool> IsAnyStudentInNotClosedOrder(IEnumerable<StudentModel> students)
    {
        using var conn = await Utils.GetAndOpenConnectionFactory();
        var cmdText = "SELECT bool_and(orders.is_closed) as all_students_in_closed, COUNT(student_flow.id) AS count_ids FROM student_flow " +
        " JOIN orders on student_flow.order_id = orders.id" +
        " WHERE student_flow.student_id = ANY(@p1)";
        using var cmd = new NpgsqlCommand(cmdText, conn);
        var parameter = new NpgsqlParameter();
        parameter.NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Array | NpgsqlTypes.NpgsqlDbType.Integer;
        parameter.Value = students.Select(x => x.Id).ToArray();
        parameter.ParameterName = "p1";
        cmd.Parameters.Add(parameter);
        using var reader = await cmd.ExecuteReaderAsync();
        reader.Read();
        if ((long)reader["count_ids"] == 0)
        {
            return false;
        }
        return !(bool)reader["all_students_in_closed"];
    }

    public GroupModel? GetCurrentGroup()
    {
        return GetLastRecord()?.GroupTo;
    }

    private StudentStates GetStudentState(out int cycleCount)
    {
        // если баланс = 0, студент не зачислен и не отчислен
        // если баланс %10 == 1 , то студент зачислен, но не отчислен
        // если баланс /10 != 0, то студент прошел круг зачисления, отчисления (отчислен) 
        int balance = 0;
        foreach (var record in _history)
        {
            var details = record.ByOrder.GetOrderTypeDetails();
            if (details.IsAnyEnrollment())
            {
                balance += 1;
            }
            else if (details.IsAnyDeduction())
            {
                balance += 9;
            }
        }
        cycleCount = balance / 10;
        if (balance == 0)
        {
            return StudentStates.NotRecorded;
        }
        if (balance % 10 == 1)
        {
            return StudentStates.Enlisted;
        }
        if (balance / 10 == 0)
        {
            return StudentStates.Deducted;
        }
        throw new Exception("Ошибка целостности истории приказов");
    }

    public bool IsStudentEnlisted()
    {
        return GetStudentState(out int cc) == StudentStates.Enlisted;
    }
    public bool IsStudentNotRecorded()
    {
        return GetStudentState(out int cc) == StudentStates.NotRecorded;
    }
    public bool IsStudentDeducted()
    {
        return GetStudentState(out int cc) == StudentStates.Deducted;
    }
    //количество полных кругов отчисления, зачисления
    public int GetCycleCount()
    {
        GetStudentState(out int cc);
        return cc;
    }

    public bool IsEnlistedInPeriod(DateTime previous, DateTime next)
    {
        foreach (var order in _history.Select(rec => rec.ByOrder))
        {
            if (order.EffectiveDate >= previous && order.EffectiveDate <= next && order.GetOrderTypeDetails().IsAnyEnrollment())
            {
                return true;
            }
        }
        return false;
    }
    public bool IsEnlistedInStandardPeriod()
    {
        return IsEnlistedInPeriod(CurrentPeriodStartDate, CurrentPeriodStartDate);
    }

    public static async Task<bool> IsStudentEnlisted(StudentModel student)
    {
        return (await StudentHistory.Create(student)).IsStudentEnlisted();
    }
    // параметер оставлен в случае, если потребуется
    // добавить опциональные приказы
    private static async Task<IEnumerable<StudentFlowRecord>> GetHistoryAggregate(QueryLimits limits,
        ComplexWhereCondition? addtionalFilter = null,
        SQLParameterCollection? parameters = null,
        DateTime? before = null,
        bool queryLastOnly = true,
        bool addOrders = false)
    {
        string alias = "outer_table";
        var studentMapper = StudentModel.GetMapper(true, new Column("student_id", alias), JoinSection.JoinType.FullJoin);
        var groupMapper = GroupModel.GetMapper(new Column("group_id_to", alias), JoinSection.JoinType.LeftJoin);
        var orderMapper = addOrders ? 
        Order.GetMapper(new Column("order_id", alias), JoinSection.JoinType.LeftJoin) :
        null;

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
                var student = studentMapper.Map(m).ResultObject;
                var groupFound = groupMapper.Map(m);
                var group = groupFound.IsFound ? groupFound.ResultObject : null;
                var orderFound = orderMapper?.Map(m);
                var orderRecieved = orderFound is null || !orderFound.IsFound ? null : orderFound.ResultObject;
                return QueryResult<StudentFlowRecord>.Found(new StudentFlowRecord(raw, orderRecieved, student, group));
            },
            usedCols
        );
        historyMapper.AssumeChild(studentMapper);
        historyMapper.AssumeChild(groupMapper);

        if (addOrders)
        {;
            historyMapper.AssumeChild(orderMapper);
        }
        else
        {
            historyMapper.PathTo.AppendJoin(
            JoinSection.JoinType.LeftJoin,
            new Column("order_id", alias),
            new Column("id", "orders")
            );
        }

        ComplexWhereCondition? basicFilter = null;
        if (queryLastOnly)
        {
            basicFilter = new ComplexWhereCondition(
            new WhereCondition(
                new Column("effective_date", "orders"),
                GetLastOrderDateSubquery(alias),
                WhereCondition.Relations.Equal
            ),
            new WhereCondition(
                new Column("order_id", alias), WhereCondition.Relations.Is
            ),
            ComplexWhereCondition.ConditionRelation.OR
            );
            if (addtionalFilter is not null)
            {
                basicFilter = new ComplexWhereCondition(basicFilter, addtionalFilter, ComplexWhereCondition.ConditionRelation.AND);
            }
        }
        else
        {
            basicFilter = addtionalFilter;
        }

        var query = 
        (queryLastOnly ? 
        SelectQuery<StudentFlowRecord>.Init("student_flow", new Column("id", "students"), alias)
        : SelectQuery<StudentFlowRecord>.Init("student_flow", alias))
        .AddMapper(historyMapper)
        .AddJoins(historyMapper.PathTo)
        .AddWhereStatement(basicFilter)
        .AddOrderByStatement(new OrderByCondition(
            new Column("id", "students"),
            OrderByCondition.OrderByTypes.ASC
        ))
        .AddParameters(parameters)
        .Finish().ResultObject;
        using var conn = await Utils.GetAndOpenConnectionFactory();
        return await query.Execute(conn, limits);
    }

    public static StudentFlowRecord? GetLastRecordOnStudent(int? studentId)
    {
        if (studentId is null)
        {
            return null;
        }
        var parameters = new SQLParameterCollection();
        var p1 = parameters.Add<int>(studentId.Value);

        var filter = new ComplexWhereCondition(
            new WhereCondition(
                new Column("id", "students"),
                p1,
                WhereCondition.Relations.Equal
            )
        );
        var found = GetHistoryAggregate(new QueryLimits(0, 1), filter, parameters).Result;
        if (found.Any())
        {
            return found.First();
        }
        else
        {
            return null;
        }
    }

    public static IEnumerable<StudentFlowRecord> GetLastRecordsForManyStudents(QueryLimits limits)
    {
        return GetHistoryAggregate(limits).Result;
    }

    public enum OrderRelationMode
    {
        OnlyExcluded,
        OnlyIncluded
    }

    public static IEnumerable<StudentFlowRecord> GetRecordsForOrder(int? orderId, OrderRelationMode mode)
    {
        if (orderId is null)
        {
            return new List<StudentFlowRecord>();
        }
        var parameters = new SQLParameterCollection();
        var p1 = parameters.Add<int>(orderId.Value);

        ComplexWhereCondition? filter;
        switch (mode)
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
                ComplexWhereCondition.ConditionRelation.OR
            );
                break;
            default:
                filter = null;
                break;
        }

        return GetHistoryAggregate(new QueryLimits(0, 500), filter, parameters, queryLastOnly: false).Result;
    }


}
