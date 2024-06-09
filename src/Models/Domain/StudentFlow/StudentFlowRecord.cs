using Contingent.Models.Domain.Orders;
using Contingent.Models.Domain.Groups;
using Contingent.Models.Domain.Students;
using Contingent.Utilities;
using Contingent.SQL;
using Npgsql;
using Contingent.Models.Infrastructure;
using System.Text;

namespace Contingent.Models.Domain.Flow;

// представляет собой запись в таблице движения студентов
// является агрегатом 

public class StudentFlowRecord
{
    private RawStudentFlowRecord _record;
    public int Id => _record.Id;
    public Period StatePeriod { get; private set; }
    public Order? ByOrder { get; private set; }
    public StudentModel? Student { get; private set; }
    public GroupModel? GroupTo { get; private set; }
    public Order OrderNullRestrict => ByOrder is null ? throw new Exception("Приказ должен быть указан") : ByOrder;
    public StudentModel StudentNullRestrict => Student is null ? throw new Exception("Студент должен быть указан") : Student;
    public GroupModel GroupToNullRestrict => GroupTo is null ? throw new Exception("Группа должен быть указана") : GroupTo;

    private StudentFlowRecord()
    {

    }

    private static StudentFlowRecord FromDatabase(
        RawStudentFlowRecord record,
        Order? order,
        StudentModel? student,
        GroupModel? group
    )
    {
        var rec = new StudentFlowRecord();
        rec._record = record;
        rec.ByOrder = order;
        rec.Student = student;
        rec.GroupTo = group;
        return rec;
    }


    public static StudentFlowRecord FromModel(Order order, StudentModel student, GroupModel? group, Period? statePeriod = null)
    {
        if (order is null || student is null)
        {
            throw new ArgumentNullException("Один из параметров записи истории не указан");
        }
        var rec = new StudentFlowRecord();
        rec.ByOrder = order;
        rec.Student = student;
        rec.GroupTo = group;
        rec._record = new RawStudentFlowRecord()
        {
            StudentId = (int)student.Id!,
            GroupToId = group?.Id,
            OrderId = (int)order.Id!,
            StartStatusDate = statePeriod?.Start,
            EndStatusDate = statePeriod?.End
        };
        return rec;
    }

    public RawStudentFlowRecord AsRaw()
    {
        return _record;
    }

    public static Mapper<StudentFlowRecord> GetMapper(
        Mapper<Order> orderMapper,
        Mapper<GroupModel> groupMapper,
        Mapper<StudentModel> studentMapper
    )
    {
        var usedCols = new List<Column> {
            new Column("id", "rec_id", "student_flow"),
            new Column("student_id", "student_flow"),
            new Column("group_id_to", "student_flow"),
            new Column("order_id", "student_flow"),
            new Column("start_status_date", "student_flow"),
            new Column("end_status_date", "student_flow")
        };

        var historyMapper = new Mapper<StudentFlowRecord>(
            (m) =>
            {
                var raw = new RawStudentFlowRecord();
                raw.Id = m["rec_id"].GetType() == typeof(DBNull) ? Utils.INVALID_ID : (int)m["rec_id"];
                raw.StudentId = m["student_id"].GetType() == typeof(DBNull) ? Utils.INVALID_ID : (int)m["student_id"];
                raw.GroupToId = m["group_id_to"].GetType() == typeof(DBNull) ? null : (int)m["group_id_to"];
                raw.OrderId = m["order_id"].GetType() == typeof(DBNull) ? Utils.INVALID_ID : (int)m["order_id"];
                raw.StartStatusDate = m["start_status_date"].GetType() == typeof(DBNull) ? null : (DateTime)m["start_status_date"];
                raw.EndStatusDate = m["end_status_date"].GetType() == typeof(DBNull) ? null : (DateTime)m["end_status_date"];
                var studentResult = studentMapper.Map(m);
                var groupResult = groupMapper.Map(m);
                var orderResult = orderMapper.Map(m);
                var group = groupResult.IsFound ? groupResult.ResultObject : null;
                var student = studentResult.IsFound ? studentResult.ResultObject : null;
                var order = orderResult.IsFound ? orderResult.ResultObject : null;
                return QueryResult<StudentFlowRecord>.Found(FromDatabase(raw, order, student, group));
            },
            usedCols
        );
        historyMapper.AssumeChild(studentMapper);
        historyMapper.AssumeChild(groupMapper);
        historyMapper.AssumeChild(orderMapper);
        return historyMapper;
    }

    public static ResultWithoutValue InsertBatch(IEnumerable<StudentFlowRecord> records, ObservableTransaction? scope)
    {
        if (records is null || !records.Any())
        {
            return ResultWithoutValue.Failure(new ValidationError("records", "Не указаны записи для проведения приказа"));
        }
        // основная запись в таблицу движения студентов
        NpgsqlConnection conn = Utils.GetAndOpenConnectionFactory().Result;
        StringBuilder query = new();
        query.Append("INSERT INTO student_flow (student_id, order_id, group_id_to, start_status_date, end_status_date) VALUES ");
        SQLParameterCollection parameters = new();

        foreach (var record in records)
        {
            var raw = record.AsRaw();
            var p1 = parameters.Add(raw.StudentId, NpgsqlTypes.NpgsqlDbType.Integer);
            var p2 = parameters.Add(raw.OrderId, NpgsqlTypes.NpgsqlDbType.Integer);
            var p3 = parameters.Add(raw.GroupToId is null ? DBNull.Value : raw.GroupToId, NpgsqlTypes.NpgsqlDbType.Integer);
            var p4 = parameters.Add(raw.StartStatusDate is null ? DBNull.Value : raw.StartStatusDate, NpgsqlTypes.NpgsqlDbType.Timestamp);
            var p5 = parameters.Add(raw.EndStatusDate is null ? DBNull.Value : raw.EndStatusDate, NpgsqlTypes.NpgsqlDbType.Timestamp);
            query.Append(string.Format("({0}, {1}, {2}, {3}, {4}),\n", p1.GetName(), p2.GetName(), p3.GetName(), p4.GetName(), p5.GetName()));
        }
        // убираем последнюю запятую и \n
        query.Remove(query.Length - 2, 1);

        var commandText = query.ToString();
        Console.WriteLine(commandText);
        NpgsqlCommand cmd;
        if (scope is null)
        {
            cmd = new NpgsqlCommand(commandText, conn);
        }
        else
        {
            cmd = new NpgsqlCommand(commandText, scope.Connection, scope.Transaction);
        }
        foreach (var p in parameters)
        {
            cmd.Parameters.Add(p.ToNpgsqlParameter());
        }
        using (cmd)
        {
            cmd.ExecuteNonQuery();
        }
        conn.Dispose();
        return ResultWithoutValue.Success();
    }
}


public struct RawStudentFlowRecord
{
    // id записи движения, всегда есть
    public int Id;
    // индентификатор студента, всегда есть 
    public int StudentId;
    // группа, КУДА переводится студент
    public int? GroupToId;
    // приказ, всегда есть
    public int OrderId;
    // дата начала состояния студента по приказу, не обязательно принадлежит конкретному приказу
    public DateTime? StartStatusDate;
    // дата окончания состояния студента по приказу
    public DateTime? EndStatusDate;

    public RawStudentFlowRecord()
    {
        Id = Utils.INVALID_ID;
        StudentId = Utils.INVALID_ID;
        GroupToId = null;
        OrderId = Utils.INVALID_ID;
        StartStatusDate = null;
        EndStatusDate = null;
    }

    public RawStudentFlowRecord(int id, int studentId, int? groupToId, int orderId, DateTime? startStatusDate, DateTime? endStatusDate)
    {
        Id = id;
        StudentId = studentId;
        GroupToId = groupToId;
        OrderId = orderId;
        StartStatusDate = startStatusDate;
        EndStatusDate = endStatusDate;
    }
}